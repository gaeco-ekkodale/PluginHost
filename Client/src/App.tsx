// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { BrowserRouter } from 'react-router-dom'
import ShellRoutes from './routes/routes'
import { OpenAPI } from './api/core/OpenAPI'
import { ProtectedApp } from './ProtectedApp'
import { AuthProvider } from 'react-oidc-context'
import { SnackbarProvider } from './context/SnackbarProvider'
import {
	MutationCache,
	QueryCache,
	QueryClient,
	QueryClientProvider,
} from '@tanstack/react-query'
import { appUserManager } from './auth/appUserManager'
import {
	handleAuthFailure,
	isAuthError,
	startAuthSessionWatch,
} from './auth/authSession'

OpenAPI.BASE = import.meta.env.VITE_HOST_API_URL

// Resolve the bearer token lazily on every request so no call can go out
// before the token is available (avoids the unauthenticated first request)
// and silent refreshes are picked up automatically.
OpenAPI.TOKEN = async () => {
	const user = await appUserManager.getUser()
	return user?.access_token ?? ''
}

// React to token expiry, failed renews and sign-outs at the identity provider.
startAuthSessionWatch()

/**
 * Turns every unauthenticated API response into a renew or a sign-out, no matter
 * which query or mutation ran into it.
 */
const handleApiError = (error: unknown) => {
	if (!isAuthError(error)) return
	void handleAuthFailure(
		'An API request was rejected as unauthenticated.',
		error
	)
}

const queryClient = new QueryClient({
	defaultOptions: {
		queries: {
			// Retrying an unauthenticated request only produces more 401s; the
			// auth handling above decides what happens instead.
			retry: (failureCount, error) =>
				!isAuthError(error) && failureCount < 3,
		},
	},
	queryCache: new QueryCache({ onError: handleApiError }),
	mutationCache: new MutationCache({ onError: handleApiError }),
})

export default function App() {
	return (
		<AuthProvider
			userManager={appUserManager}
			onSigninCallback={() => {
				window.history.replaceState(
					{},
					document.title,
					window.location.pathname
				)
			}}
		>
			<ProtectedApp>
				<BrowserRouter>
					<SnackbarProvider>
						<QueryClientProvider client={queryClient}>
							<ShellRoutes />
						</QueryClientProvider>
					</SnackbarProvider>
				</BrowserRouter>
			</ProtectedApp>
		</AuthProvider>
	)
}
