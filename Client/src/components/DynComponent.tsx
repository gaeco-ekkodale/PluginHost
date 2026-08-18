// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import React, {
	Suspense,
	useEffect,
	useState,
	useCallback,
	useMemo,
	useRef,
} from 'react'
import {
	__federation_method_getRemote,
	__federation_method_setRemote,
	__federation_method_unwrapDefault,
} from 'virtual:__federation__'
import { CircularProgress } from '@mui/material'
import { GetPluginDto, PluginsService } from '../api'
import ErrorBoundary from './ErrorBoundary'
import { AuthProvider } from 'react-oidc-context'
import { User, UserManager, WebStorageStateStore } from 'oidc-client-ts'
import { appUserManager } from '../auth/appUserManager'
import {
	handleAuthFailure,
	isAuthError,
	isAuthSessionActive,
} from '../auth/authSession'
import { PluginProps } from '../context/PluginProps'

export type DynComponentProps = {
	plugin: GetPluginDto
	pluginProps?: PluginProps
}

const Plugin = ({ plugin, pluginProps }: DynComponentProps) => {
	const [Component, setComponent] =
		useState<React.ComponentType<PluginProps> | null>(null)
	const [error, setError] = useState<string | null>(null)
	const [isLoading, setIsLoading] = useState<boolean>(true)
	const isExchangingTokenRef = useRef(false)

	const userManager = useMemo(
		() =>
			new UserManager({
				authority: `${import.meta.env.VITE_KEYCLOAK_URL}/realms/${import.meta.env.VITE_KEYCLOAK_REALM}`,
				client_id: 'plugin',
				redirect_uri: `${window.location.origin}${window.location.pathname}`,
				userStore: new WebStorageStateStore({
					store: window.sessionStorage,
				}),
				automaticSilentRenew: false,
			}),
		[]
	)

	const exchangeToken = useCallback(async () => {
		if (isExchangingTokenRef.current) {
			return
		}

		isExchangingTokenRef.current = true

		const currentUser = await appUserManager.getUser()
		if (!currentUser) {
			isExchangingTokenRef.current = false
			throw new Error('No user available for token exchange')
		}

		try {
			const tokenResponse = await PluginsService.exchangeTokenEndpoint(
				plugin.id,
				{ accessToken: currentUser.access_token }
			)

			const user = new User({
				session_state: tokenResponse.session_state || '',
				access_token: tokenResponse.access_token,
				token_type: tokenResponse.token_type || 'Bearer',
				scope: tokenResponse.scope || '',
				profile: currentUser.profile,
				expires_at:
					Math.floor(Date.now() / 1000) + tokenResponse.expires_in,
			})

			await userManager.storeUser(user)
			await userManager.events.load(user, true)
		} finally {
			isExchangingTokenRef.current = false
		}
	}, [plugin.id, userManager])

	useEffect(() => {
		let isMounted = true

		setIsLoading(true)
		setError(null)
		setComponent(null)

		const loadPlugin = async (isRetry = false) => {
			try {
				await exchangeToken()

				__federation_method_setRemote(plugin.id, {
					url: () => Promise.resolve(plugin.url),
					format: 'esm',
					from: 'vite',
				})

				const module = await __federation_method_getRemote(
					plugin.id,
					`./${plugin.module}`
				)
				const loadedComponent =
					await __federation_method_unwrapDefault(module)

				if (isMounted) {
					setComponent(
						() =>
							loadedComponent as React.ComponentType<PluginProps>
					)
					setIsLoading(false)
				}
			} catch (loadError) {
				// The token exchange runs against the shell API, so an auth error
				// here means the shell session - not the module - is the problem.
				// Renewing or signing out is handled centrally; on a successful
				// renew the load is worth exactly one more attempt.
				if (!isRetry && isAuthError(loadError)) {
					await handleAuthFailure(
						`Module "${plugin.name}" could not be loaded.`,
						loadError
					)
					if (isMounted && isAuthSessionActive()) {
						await loadPlugin(true)
					}
					return
				}

				if (isMounted) {
					setError(`Module "${plugin.name}" could not be loaded.`)
					setIsLoading(false)
				}
			}
		}

		void loadPlugin()

		return () => {
			isMounted = false
		}
	}, [exchangeToken, plugin.id, plugin.url, plugin.module, plugin.name])

	useEffect(() => {
		const handleHostUserLoaded = async () => {
			try {
				await exchangeToken()
			} catch {
				// Host renew failures are handled by the host auth flow.
			}
		}

		appUserManager.events.addUserLoaded(handleHostUserLoaded)

		return () => {
			appUserManager.events.removeUserLoaded(handleHostUserLoaded)
		}
	}, [exchangeToken])

	/**
	 * Keeps one continuous loading state from mount until the remote is on screen.
	 * The remote is fetched, its token exchanged and its own data loaded afterwards, so
	 * anything less than a spinner here reads as a broken page rather than as progress.
	 */
	const loadingFallback = (
		<div className='flex h-full w-full items-center justify-center py-16'>
			<CircularProgress />
		</div>
	)

	return (
		<Suspense fallback={loadingFallback}>
			<ErrorBoundary pluginName={plugin.name} externalError={error}>
				<AuthProvider userManager={userManager}>
					{Component ? (
						<Component {...(pluginProps ?? {})} />
					) : isLoading ? (
						loadingFallback
					) : (
						<div className='flex h-full w-full items-center justify-center py-16 text-gray-600'>
							Module could not be loaded.
						</div>
					)}
				</AuthProvider>
			</ErrorBoundary>
		</Suspense>
	)
}

const arePropsEqual = (
	prevProps: { plugin: GetPluginDto },
	nextProps: { plugin: GetPluginDto }
) => {
	return (
		prevProps.plugin.id === nextProps.plugin.id &&
		prevProps.plugin.module === nextProps.plugin.module &&
		prevProps.plugin.route === nextProps.plugin.route
	)
}

// Memoize the component to prevent unnecessary re-renders
export default React.memo(Plugin, arePropsEqual)
