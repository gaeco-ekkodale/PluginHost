// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import React, { createContext, useContext, useState, useCallback } from 'react'
import { Snackbar, Alert, Button } from '@mui/material'

/**
 * Type definition for a snackbar item
 */
type SnackbarItem = {
	id: string
	message: string
	severity: 'info' | 'success' | 'warning' | 'error'
	actionLabel?: string
	onAction?: () => void
	autoHideDuration: number | null
}

type SnackbarOptions = {
	actionLabel?: string
	onAction?: () => void
	autoHideDuration?: number
	persist?: boolean
}

/**
 * Type definition for the snackbar context function
 * @param message - The message to display
 * @param severity - Optional severity level of the snackbar
 */
export type SnackbarContextType = (
	message: string,
	severity?: 'info' | 'success' | 'warning' | 'error',
	options?: SnackbarOptions
) => void

/**
 * Context that provides a function to show snackbars
 */
const SnackbarContext = createContext<SnackbarContextType>(() => {})

/**
 * Custom hook for easy access to the snackbar system
 * @returns A function to display snackbars
 */
export function useSnackbar() {
	return useContext(SnackbarContext)
}

/**
 * Provider component that manages the snackbar queue system
 * @param children - React children components
 */
export function SnackbarProvider({ children }: { children: React.ReactNode }) {
	const [snackbars, setSnackbars] = useState<SnackbarItem[]>([])

	/**
	 * Adds a new snackbar to the queue
	 * @param message - The message to display
	 * @param severity - The severity level of the snackbar
	 */
	const showSnackbar = useCallback(
		(
			message: string,
			severity: 'info' | 'success' | 'warning' | 'error' = 'info',
			options?: SnackbarOptions
		) => {
			const newSnackbar: SnackbarItem = {
				id: crypto.randomUUID(),
				message,
				severity,
				actionLabel: options?.actionLabel,
				onAction: options?.onAction,
				autoHideDuration: options?.persist
					? null
					: (options?.autoHideDuration ?? 6000),
			}

			setSnackbars(prev => [...prev, newSnackbar])
		},
		[]
	)

	const closeSnackbar = useCallback((id: string) => {
		setSnackbars(prev => prev.filter(item => item.id !== id))
	}, [])

	/**
	 * Handles closing the current snackbar
	 * @param _event - The event that triggered the close
	 * @param reason - The reason for closing
	 */
	const visibleSnackbars = snackbars.slice(-5)

	return (
		<SnackbarContext.Provider value={showSnackbar}>
			{children}
			{visibleSnackbars.map((item, index) => {
				const stackOffset = 24 + index * 72

				return (
					<Snackbar
						key={item.id}
						open
						autoHideDuration={item.autoHideDuration}
						onClose={(_event, reason) => {
							if (reason === 'clickaway') return
							closeSnackbar(item.id)
						}}
						anchorOrigin={{
							vertical: 'bottom',
							horizontal: 'right',
						}}
						sx={{
							bottom: `${stackOffset}px !important`,
						}}
					>
						<Alert
							onClose={() => closeSnackbar(item.id)}
							severity={item.severity}
							sx={{ width: '100%' }}
							variant='filled'
							action={
								item.actionLabel && item.onAction ? (
									<Button
										color='inherit'
										size='small'
										onClick={() => {
											item.onAction?.()
											closeSnackbar(item.id)
										}}
									>
										{item.actionLabel}
									</Button>
								) : undefined
							}
						>
							{item.message}
						</Alert>
					</Snackbar>
				)
			})}
		</SnackbarContext.Provider>
	)
}
