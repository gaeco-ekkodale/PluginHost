// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {
	Alert,
	Box,
	Button,
	CircularProgress,
	Container,
	Paper,
	Stack,
	Typography,
} from '@mui/material'
import { type ReactNode, useEffect, useState } from 'react'
import { hasAuthParams, useAuth } from 'react-oidc-context'
import {
	describeAuthError,
	handleAuthFailure,
	isUnrecoverableAuthError,
	restartSignIn,
	signOut,
	useAuthSessionState,
} from './auth/authSession'
interface ProtectedAppProps {
	children: ReactNode
}

/**
 * Full-screen states while the shell is not usable. Everything here is a dead end
 * unless the user gets an action, so each state offers a way back to a sign-in.
 */
const AuthScreen: React.FC<{
	title: string
	severity: 'error' | 'warning' | 'info'
	message: string
	hint?: string
	actions?: ReactNode
}> = ({ title, severity, message, hint, actions }) => (
	<Container maxWidth='sm' sx={{ mt: 5 }}>
		<Paper elevation={3} sx={{ p: 4, borderRadius: 2 }}>
			<Typography
				variant='h4'
				component='h1'
				gutterBottom
				color={severity === 'info' ? 'text.primary' : 'error'}
			>
				{title}
			</Typography>
			<Alert severity={severity} variant='filled' sx={{ mt: 2 }}>
				{message}
			</Alert>
			{hint && (
				<Typography
					variant='body2'
					sx={{ mt: 3, color: 'text.secondary' }}
				>
					{hint}
				</Typography>
			)}
			{actions && (
				<Stack
					direction='row'
					spacing={2}
					sx={{ mt: 3, justifyContent: 'center' }}
				>
					{actions}
				</Stack>
			)}
		</Paper>
	</Container>
)

const FullScreenSpinner: React.FC<{ label: string }> = ({ label }) => (
	<Box
		sx={{
			display: 'flex',
			flexDirection: 'column',
			alignItems: 'center',
			justifyContent: 'center',
			height: '100vh',
		}}
	>
		<CircularProgress size={60} thickness={4} />
		<Typography variant='h5' sx={{ mt: 3 }}>
			{label}
		</Typography>
	</Box>
)

export const ProtectedApp: React.FC<ProtectedAppProps> = ({ children }) => {
	const auth = useAuth()
	const session = useAuthSessionState()

	const [hasTriedSignin, setHasTriedSignin] = useState(false)
	const [hasAuthenticatedSession, setHasAuthenticatedSession] =
		useState(false)

	useEffect(() => {
		if (auth.isAuthenticated && auth.user?.access_token) {
			setHasAuthenticatedSession(true)
		}
	}, [auth.isAuthenticated, auth.user?.access_token])

	const isSilentRefreshInProgress =
		hasAuthenticatedSession &&
		!auth.error &&
		session.status === 'active' &&
		(auth.isLoading || auth.activeNavigator === 'signinSilent')

	/**
	 * A sign-in error that names a dead session must end the session instead of
	 * parking the user on an error screen with a token that can never work again.
	 */
	useEffect(() => {
		if (!auth.error || session.status !== 'active') return
		if (!isUnrecoverableAuthError(auth.error)) return

		void handleAuthFailure('Sign-in failed.', auth.error)
	}, [auth.error, session.status])

	/**
	 * A stored user that is already expired keeps the shell unauthenticated
	 * without any error - renew it or sign out.
	 */
	useEffect(() => {
		if (session.status !== 'active') return
		if (auth.error || auth.isLoading || auth.activeNavigator) return
		if (!auth.user || auth.isAuthenticated) return

		void handleAuthFailure('The stored session is expired.')
	}, [
		auth.user,
		auth.isAuthenticated,
		auth.isLoading,
		auth.activeNavigator,
		auth.error,
		session.status,
	])

	useEffect(() => {
		if (session.status !== 'active') return
		if (
			!(
				hasAuthParams() ||
				auth.isAuthenticated ||
				auth.activeNavigator ||
				auth.isLoading ||
				auth.error ||
				auth.user ||
				hasTriedSignin
			)
		) {
			auth.signinRedirect()
			setHasTriedSignin(true)
		}
	}, [
		auth.isAuthenticated,
		auth.activeNavigator,
		auth.isLoading,
		auth.user,
		hasTriedSignin,
		session.status,
	])

	if (session.status === 'signing-out') {
		return <FullScreenSpinner label='Signing out…' />
	}

	// The session ended repeatedly right after signing in - stop redirecting and
	// let the user decide, otherwise the shell bounces to Keycloak forever.
	if (session.status === 'failed') {
		return (
			<AuthScreen
				title='Session ended'
				severity='error'
				message='Your session is no longer valid.'
				hint={`Signing in again did not restore a working session. Please try once more or contact your administrator.${session.reason ? ` Technical detail: ${session.reason}` : ''}`}
				actions={
					<>
						<Button
							variant='contained'
							onClick={() => void restartSignIn()}
						>
							Sign in again
						</Button>
						<Button
							variant='outlined'
							onClick={() => void signOut()}
						>
							Sign out
						</Button>
					</>
				}
			/>
		)
	}

	if (isSilentRefreshInProgress) {
		return <>{children}</>
	}

	// Error state
	if (auth.error) {
		return (
			<AuthScreen
				title='Sign-in failed'
				severity='error'
				message={describeAuthError(auth.error) || auth.error.message}
				hint='Please try again in a moment, or contact your administrator.'
				actions={
					<>
						<Button
							variant='contained'
							onClick={() => void restartSignIn()}
						>
							Sign in again
						</Button>
						<Button
							variant='outlined'
							onClick={() => void signOut()}
						>
							Sign out
						</Button>
					</>
				}
			/>
		)
	}

	// Loading state before first successful authentication
	if (auth.isLoading && !auth.isAuthenticated) {
		return <FullScreenSpinner label='Loading…' />
	}

	// Authenticated state
	if (auth.isAuthenticated && auth.user?.access_token) {
		return <>{children}</>
	}

	// Default error state (unable to sign in)
	return (
		<AuthScreen
			title='Not signed in'
			severity='warning'
			message='Sign-in was not possible.'
			hint='If the problem persists, please contact your administrator.'
			actions={
				<Button
					variant='contained'
					onClick={() => void restartSignIn()}
				>
					Sign in
				</Button>
			}
		/>
	)
}
