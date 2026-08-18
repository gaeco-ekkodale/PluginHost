// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useCallback, useMemo } from 'react'
import { Route, Routes } from 'react-router-dom'
import { CircularProgress } from '@mui/material'

import Navbar from '../components/Navbar'
import RootContent from '../components/RootContent'
import DynComponent from '../components/DynComponent'
import { SignalREvent } from '../features/PluginUpdate/hooks/useSignalRConnection'
import { renewSession } from '../auth/authSession'

import { usePlugins } from './hooks'
import { PluginProps } from '../context/PluginProps'
import { useSnackbar } from '../context/SnackbarProvider'

const Mainpage = () => {
	const showSnackbar = useSnackbar()
	const pluginProps = useMemo<PluginProps>(
		() => ({
			showSnackbar,
		}),
		[showSnackbar]
	)

	const handleSignalREvent = useCallback((event: SignalREvent) => {
		if (event.changeType !== 'catalog') return

		if (event.requiresTokenRefresh ?? true) {
			// The catalog changed, so the token's audiences are stale. renewSession
			// coalesces bursts of events into a single silent renew.
			void renewSession()
		}
	}, [])

	const {
		menuGroups,
		storedPlugins,
		homepagePlugin,
		hasLoaded,
		loadFailed,
		refetch,
	} = usePlugins({
		onSignalREvent: handleSignalREvent,
		onPluginListChanged: ({ addedPluginIds, removedPluginIds }) => {
			const parts = [
				addedPluginIds.length ? `${addedPluginIds.length} added` : null,
				removedPluginIds.length
					? `${removedPluginIds.length} removed`
					: null,
			]
				.filter(Boolean)
				.join(', ')

			showSnackbar(
				parts
					? `App catalog updated (${parts}).`
					: 'App catalog updated.',
				'info'
			)
		},
	})
	// Routes must not mount before the plugin list is known. Otherwise no route matches
	// the requested plugin path yet and the catch-all renders a 404 for a few frames -
	// visible as a flash whenever a plugin is opened directly or the page is reloaded.
	// `isLoading` is not enough: while the query is still disabled (no token yet) it is
	// false even though nothing has been fetched.
	const showInitialLoader = !hasLoaded

	return (
		<div className='flex h-screen flex-col overflow-hidden'>
			<Navbar
				menuGroups={menuGroups}
				hasLoaded={hasLoaded}
				loadFailed={loadFailed}
				onLayoutSaved={refetch}
				onReload={refetch}
			/>
			{showInitialLoader ? (
				<div className='flex h-full w-full flex-col items-center justify-center gap-2'>
					<CircularProgress />
					<div className='text-gray-600'>Loading applications…</div>
				</div>
			) : (
				<Routes>
					<Route
						path='/'
						element={
							homepagePlugin ? (
								<div className='w-full flex-1 overflow-auto'>
									<DynComponent
										plugin={homepagePlugin}
										key={homepagePlugin.id}
										pluginProps={pluginProps}
									/>
								</div>
							) : (
								<RootContent />
							)
						}
					/>
					{storedPlugins?.map(plugin => (
						<Route
							key={plugin.id}
							path={`${plugin.route}/*`}
							element={
								<div className='w-full flex-1 overflow-auto'>
									<DynComponent
										plugin={plugin}
										key={plugin.id}
										pluginProps={pluginProps}
									/>
								</div>
							}
						/>
					))}

					<Route
						path='*'
						element={
							<div className='flex h-full w-full flex-col items-center justify-center gap-4'>
								<h1 className='text-4xl font-bold text-gray-800'>
									404
								</h1>
								<p className='text-xl text-gray-600'>
									This page could not be found.
								</p>
							</div>
						}
					/>
				</Routes>
			)}
		</div>
	)
}

export default Mainpage
