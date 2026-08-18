// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useEffect, useState, useCallback, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from 'react-oidc-context'

import { GetPluginDto, PluginMenuGroupDto, PluginMenuService } from '../../api'
import {
	SignalREvent,
	useSignalRConnection,
} from '../../features/PluginUpdate/hooks/useSignalRConnection'

interface UsePluginsOptions {
	onSignalREvent: (event: SignalREvent) => void
	onPluginListChanged?: (payload: {
		addedPluginIds: string[]
		removedPluginIds: string[]
	}) => void
}

export interface UsePluginsReturn {
	menuGroups: PluginMenuGroupDto[]
	storedPlugins: GetPluginDto[] | undefined
	homepagePlugin: GetPluginDto | undefined
	refetch: () => void
	isLoading: boolean
	/**
	 * True once the menu request has settled at least once - also when it failed,
	 * so a failing menu cannot leave the shell in an endless loading state.
	 */
	hasLoaded: boolean
	/** True when the last menu request failed. */
	loadFailed: boolean
}

/**
 * Custom hook to manage plugin fetching via plugin-menu endpoint, storage, and SignalR updates
 */
const toSortedPluginIds = (plugins: GetPluginDto[] | undefined): string[] =>
	(plugins?.map(plugin => plugin.id) ?? []).sort()

const areSortedIdListsEqual = (left: string[], right: string[]): boolean =>
	left.length === right.length &&
	left.every((value, index) => value === right[index])

export const usePlugins = ({
	onSignalREvent,
	onPluginListChanged,
}: UsePluginsOptions): UsePluginsReturn => {
	const [storedPlugins, setStoredPlugins] = useState<
		GetPluginDto[] | undefined
	>(undefined)
	const [storedMenuGroups, setStoredMenuGroups] = useState<
		PluginMenuGroupDto[]
	>([])
	const auth = useAuth()

	const {
		data: menuGroups,
		refetch,
		isLoading,
		isError,
	} = useQuery({
		queryKey: ['pluginhost-plugin-menu'],
		queryFn: () => PluginMenuService.getPluginMenu(),
		enabled: !!auth.user?.access_token,
	})

	// Flatten all plugins from all groups
	const fetchedPlugins = useMemo<GetPluginDto[]>(
		() => (menuGroups ?? []).flatMap(g => g.plugins ?? []),
		[menuGroups]
	)

	/**
	 * Sync fetched groups/plugins into stored state and notify caller on changes.
	 */
	useEffect(() => {
		if (isLoading || !menuGroups) return

		const newIds = toSortedPluginIds(fetchedPlugins)
		const oldIds = toSortedPluginIds(storedPlugins)
		const pluginsAreEqual =
			storedPlugins !== undefined && areSortedIdListsEqual(newIds, oldIds)

		if (storedPlugins === undefined) {
			setStoredPlugins(fetchedPlugins)
			setStoredMenuGroups(menuGroups)
			return
		}

		if (!pluginsAreEqual) {
			const addedPluginIds = newIds.filter(id => !oldIds.includes(id))
			const removedPluginIds = oldIds.filter(id => !newIds.includes(id))

			onPluginListChanged?.({ addedPluginIds, removedPluginIds })
			setStoredPlugins(fetchedPlugins)
		}

		setStoredMenuGroups(menuGroups)
	}, [
		fetchedPlugins,
		menuGroups,
		storedPlugins,
		isLoading,
		onPluginListChanged,
	])

	/**
	 * Homepage plugin derived from stored plugins
	 */
	const homepagePlugin = useMemo(() => {
		return storedPlugins?.find(plugin =>
			plugin.id.toLowerCase().includes('homepage')
		)
	}, [storedPlugins])

	/**
	 * Handle SignalR events
	 */
	const handleSignalREvent = useCallback(
		(event: SignalREvent) => {
			onSignalREvent(event)
			refetch()
		},
		[onSignalREvent, refetch]
	)

	useSignalRConnection(handleSignalREvent)

	/**
	 * Refetch when auth token changes
	 */
	useEffect(() => {
		refetch()
	}, [auth.user?.access_token, refetch])

	return {
		menuGroups: storedMenuGroups,
		storedPlugins,
		homepagePlugin,
		refetch,
		isLoading,
		hasLoaded: storedPlugins !== undefined || isError,
		loadFailed: isError,
	}
}
