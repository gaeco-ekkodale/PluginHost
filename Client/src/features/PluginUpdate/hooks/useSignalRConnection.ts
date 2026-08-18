// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useEffect, useRef } from 'react'
import { HubConnectionBuilder } from '@microsoft/signalr'

export interface SignalREvent {
	changeType?: 'catalog' | 'menu'
	operation: string
	message: string
	source?: string
	requiresTokenRefresh?: boolean
	addedPluginIds?: string[]
	removedPluginIds?: string[]
	addedPlugins?: Array<{ id: string; name: string; route: string }>
	removedPlugins?: Array<{ id: string; name: string; route: string }>
	totalPlugins?: number
	totalGroups?: number
	occurredAtUtc?: string
}

const pluginHostUrl = import.meta.env.VITE_HOST_API_URL
const signalRHubPath = import.meta.env.VITE_SIGNALR_PATH
const signalREvent = import.meta.env.VITE_SIGNALR_PLUGIN_EVENT

/**
 * Custom hook to manage SignalR connection for plugin updates
 * Handles connection setup, event listening, and cleanup
 */
export const useSignalRConnection = (
	onPluginUpdate: (event: SignalREvent) => void
) => {
	const onPluginUpdateRef = useRef(onPluginUpdate)
	onPluginUpdateRef.current = onPluginUpdate

	useEffect(() => {
		let isMounted = true
		const connection = new HubConnectionBuilder()
			.withUrl(pluginHostUrl + signalRHubPath, {
				withCredentials: false,
			})
			.withAutomaticReconnect()
			.build()

		connection.on(signalREvent, (...args: unknown[]) => {
			if (isMounted) {
				const firstArg = args[0]
				const secondArg = args[1]

				if (
					firstArg &&
					typeof firstArg === 'object' &&
					'operation' in firstArg &&
					'message' in firstArg
				) {
					onPluginUpdateRef.current(firstArg as SignalREvent)
					return
				}

				if (
					secondArg &&
					typeof secondArg === 'object' &&
					'operation' in secondArg &&
					'message' in secondArg
				) {
					onPluginUpdateRef.current(secondArg as SignalREvent)
					return
				}

				const operation = typeof args[0] === 'string' ? args[0] : ''
				const message = typeof args[1] === 'string' ? args[1] : ''

				onPluginUpdateRef.current({
					changeType: 'catalog',
					operation,
					message,
				})
			}
		})

		const startConnection = async () => {
			try {
				await connection.start()
				console.log('SignalR Connected')
			} catch (err) {
				console.error('SignalR connection error: ', err)
				// Optional: Retry logic or notify user
			}
		}

		startConnection()

		/**
		 * Cleanup on unmount
		 */
		return () => {
			isMounted = false
			connection.stop().catch(err => {
				console.error('Error closing SignalR connection:', err)
			})
		}
	}, [])
}
