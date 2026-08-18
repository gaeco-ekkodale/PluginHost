// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useSyncExternalStore } from 'react'
import { ApiError } from '../api'
import { appUserManager } from './appUserManager'

/**
 * Central handling of auth failures in the shell.
 *
 * Every auth failure - a 401 from the API, an expired access token, a Keycloak
 * session that is no longer active - must end in exactly one of two outcomes:
 * a successful silent renew, or a sign-out. Anything in between leaves the user
 * in a half-authenticated shell that keeps firing requests that cannot succeed.
 */

/** OIDC/Keycloak errors that mean the session cannot be renewed at all. */
const UNRECOVERABLE_AUTH_ERRORS = [
	'login_required',
	'consent_required',
	'interaction_required',
	'invalid_grant',
	'invalid_token',
	'session not active',
	'session_not_active',
	'token is not active',
	'not_active',
	'user session not found',
]

/**
 * Marks that this tab already ended a session. Kept in sessionStorage because it
 * has to survive the redirect to Keycloak and back.
 */
const FORCED_SIGN_OUT_MARKER = 'pluginhost.forcedSignOutAt'

/** Two forced sign-outs within this window are treated as a redirect loop. */
const SIGN_OUT_LOOP_WINDOW_MS = 30_000

/** Silent renews are coalesced within this window instead of being repeated. */
const RENEW_THROTTLE_MS = 2_500

/** A failure this soon after a fresh token was issued cannot be fixed by renewing. */
const RENEW_GRACE_MS = 10_000

export type AuthSessionStatus = 'active' | 'signing-out' | 'failed'

export interface AuthSessionState {
	status: AuthSessionStatus
	/** Technical reason of the last transition, for the error screen and logs. */
	reason?: string
}

let state: AuthSessionState = { status: 'active' }
const listeners = new Set<() => void>()

let renewInFlight: Promise<boolean> | null = null
let lastRenew: { at: number; ok: boolean } | null = null
let lastSuccessfulRenewAt = 0
let isSigningOut = false
let isWatching = false

const setState = (next: AuthSessionState) => {
	state = next
	listeners.forEach(listener => listener())
}

const subscribe = (listener: () => void) => {
	listeners.add(listener)
	return () => {
		listeners.delete(listener)
	}
}

/**
 * Subscribes a component to the current auth session status.
 */
export const useAuthSessionState = (): AuthSessionState =>
	useSyncExternalStore(subscribe, () => state)

/**
 * True while the shell has a usable session, i.e. no sign-out or terminal auth
 * failure is being handled. Use it before retrying work that needs a token.
 */
export const isAuthSessionActive = (): boolean =>
	state.status === 'active' && !isSigningOut

/**
 * Flattens anything that was thrown into searchable text (Error, OIDC
 * ErrorResponse, plain object or string).
 */
export const describeAuthError = (error: unknown): string => {
	if (!error) return ''
	if (typeof error === 'string') return error
	if (typeof error === 'object') {
		const candidate = error as {
			name?: unknown
			message?: unknown
			error?: unknown
			error_description?: unknown
			status?: unknown
			body?: unknown
		}
		const body =
			typeof candidate.body === 'string' ? candidate.body : undefined
		return [
			candidate.name,
			candidate.error,
			candidate.error_description,
			candidate.message,
			candidate.status !== undefined
				? `status ${candidate.status}`
				: undefined,
			body,
		]
			.filter(part => typeof part === 'string' && part.length > 0)
			.join(' ')
	}
	return String(error)
}

/**
 * True for errors that mean the current session is dead and cannot be renewed.
 */
export const isUnrecoverableAuthError = (error: unknown): boolean => {
	const text = describeAuthError(error).toLowerCase()
	return UNRECOVERABLE_AUTH_ERRORS.some(code => text.includes(code))
}

/**
 * True for errors that must be answered with a renew or a sign-out.
 * A 403 is deliberately not an auth failure: the user is authenticated but not
 * permitted, and signing them out would hide that difference.
 */
export const isAuthError = (error: unknown): boolean => {
	if (error instanceof ApiError) return error.status === 401
	return isUnrecoverableAuthError(error)
}

const hasRecentForcedSignOut = (): boolean => {
	try {
		const marker = window.sessionStorage.getItem(FORCED_SIGN_OUT_MARKER)
		if (!marker) return false
		return Date.now() - Number(marker) < SIGN_OUT_LOOP_WINDOW_MS
	} catch {
		return false
	}
}

const markForcedSignOut = () => {
	try {
		window.sessionStorage.setItem(
			FORCED_SIGN_OUT_MARKER,
			String(Date.now())
		)
	} catch {
		// Storage is not essential - without it we only lose loop detection.
	}
}

/**
 * Drops every stored OIDC user of this tab. Besides the shell user those are the
 * exchanged per-plugin tokens, which would otherwise outlive the session they
 * were issued for.
 */
const clearStoredOidcUsers = () => {
	try {
		const store = window.sessionStorage
		Object.keys(store)
			.filter(key => key.startsWith('oidc.user:'))
			.forEach(key => store.removeItem(key))
	} catch {
		// Nothing we can do if storage is unavailable.
	}
}

const runRenew = async (): Promise<boolean> => {
	try {
		const user = await appUserManager.signinSilent()
		return !!user?.access_token
	} catch (error) {
		console.warn('[auth] Silent renew failed:', error)
		return false
	}
}

/**
 * Renews the session silently. Concurrent callers share one attempt and repeated
 * calls within a short window reuse its result, so a burst of failing requests
 * cannot start a burst of renews.
 */
export const renewSession = async (): Promise<boolean> => {
	if (renewInFlight) return renewInFlight
	if (lastRenew && Date.now() - lastRenew.at < RENEW_THROTTLE_MS) {
		return lastRenew.ok
	}

	renewInFlight = runRenew()
	const ok = await renewInFlight
	renewInFlight = null
	lastRenew = { at: Date.now(), ok }
	if (ok) lastSuccessfulRenewAt = lastRenew.at
	return ok
}

/**
 * Ends the session: drops the local user and sends the browser to the Keycloak
 * logout endpoint. Falls back to a local reset if the identity provider cannot
 * be reached, and refuses to bounce the user in a sign-in/sign-out loop.
 */
export const forceSignOut = async (
	reason: string,
	options: { userInitiated?: boolean } = {}
): Promise<void> => {
	if (isSigningOut) return
	isSigningOut = true
	console.warn(`[auth] Ending session: ${reason}`)

	if (!options.userInitiated && hasRecentForcedSignOut()) {
		// We already ended a session moments ago and are in the same state again.
		// Redirecting once more would bounce between Keycloak and the shell, so
		// hand over to the user instead.
		setState({ status: 'failed', reason })
		isSigningOut = false
		return
	}

	setState({ status: 'signing-out', reason })
	if (!options.userInitiated) markForcedSignOut()

	let idTokenHint: string | undefined
	try {
		idTokenHint = (await appUserManager.getUser())?.id_token
	} catch {
		// Without the hint Keycloak just ends the session without confirmation.
	}

	try {
		appUserManager.stopSilentRenew()
	} catch {
		// Nothing to stop.
	}

	try {
		// Drop local state first so a failing redirect cannot leave a stale user
		// behind that the shell would treat as signed in.
		await appUserManager.removeUser()
	} catch (error) {
		console.warn('[auth] Could not clear the local user:', error)
	}
	clearStoredOidcUsers()

	try {
		await appUserManager.signoutRedirect(
			idTokenHint ? { id_token_hint: idTokenHint } : undefined
		)
		return
	} catch (error) {
		console.warn(
			'[auth] Sign-out redirect failed, resetting locally instead:',
			error
		)
	}

	// Local state is already gone; a reload restarts the sign-in flow cleanly.
	window.location.replace(window.location.origin)
}

/**
 * Single entry point for auth failures: renew once if there is any chance of
 * recovery, otherwise sign the user out.
 */
export const handleAuthFailure = async (
	context: string,
	error?: unknown
): Promise<void> => {
	if (state.status !== 'active' || isSigningOut) return

	const details = describeAuthError(error)
	const reason = details ? `${context} (${details})` : context

	if (error !== undefined && isUnrecoverableAuthError(error)) {
		await forceSignOut(reason)
		return
	}

	if (Date.now() - lastSuccessfulRenewAt < RENEW_GRACE_MS) {
		// The token was just refreshed and is still rejected - renewing again
		// would only loop.
		await forceSignOut(reason)
		return
	}

	if (await renewSession()) return
	await forceSignOut(reason)
}

/**
 * Starts a fresh sign-in from a clean state. Used by the recovery buttons on the
 * auth error screens.
 */
export const restartSignIn = async (): Promise<void> => {
	try {
		window.sessionStorage.removeItem(FORCED_SIGN_OUT_MARKER)
	} catch {
		// Ignore - only affects loop detection.
	}

	isSigningOut = false
	lastRenew = null
	lastSuccessfulRenewAt = 0
	setState({ status: 'active' })

	try {
		await appUserManager.removeUser()
	} catch {
		// Continue - the redirect below issues a new session anyway.
	}
	clearStoredOidcUsers()

	try {
		await appUserManager.signinRedirect()
	} catch (error) {
		console.warn('[auth] Sign-in redirect failed:', error)
		window.location.replace(window.location.origin)
	}
}

/**
 * Signs out on the user's request (bypasses the loop guard).
 */
export const signOut = (): Promise<void> =>
	forceSignOut('The user signed out.', { userInitiated: true })

/**
 * Reacts to session events of the identity provider. Registered once for the
 * lifetime of the shell.
 */
export const startAuthSessionWatch = (): void => {
	if (isWatching) return
	isWatching = true

	// The access token expired without a renew taking its place.
	appUserManager.events.addAccessTokenExpired(() => {
		void handleAuthFailure('The access token expired.')
	})

	// Automatic renew failed - usually because the session is no longer active.
	appUserManager.events.addSilentRenewError(error => {
		void handleAuthFailure('The session could not be renewed.', error)
	})

	// The session monitor noticed a sign-out at the identity provider, e.g. in
	// another tab or triggered by an administrator.
	appUserManager.events.addUserSignedOut(() => {
		void forceSignOut('The session ended at the identity provider.')
	})
}
