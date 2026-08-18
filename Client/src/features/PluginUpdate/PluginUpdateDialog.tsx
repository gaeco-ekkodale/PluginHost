// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import React from 'react'
import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	Typography,
} from '@mui/material'
import SystemUpdateAltIcon from '@mui/icons-material/SystemUpdateAlt'

interface PluginUpdateDialogProps {
	open: boolean
	message: string
	onContinue: () => void
	onUpdate: () => void
}

const PluginUpdateDialog: React.FC<PluginUpdateDialogProps> = ({
	open,
	message,
	onContinue,
	onUpdate,
}) => {
	return (
		<Dialog open={open} onClose={onContinue} maxWidth='sm' fullWidth>
			<DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
				<SystemUpdateAltIcon color='warning' />
				Module change detected
			</DialogTitle>
			<DialogContent dividers>
				{message && (
					<Typography
						variant='body1'
						fontWeight='medium'
						gutterBottom
					>
						{message}
					</Typography>
				)}
				<Typography
					variant='body2'
					color='text.secondary'
					sx={{ mt: 1 }}
				>
					The list of modules has changed. Loading the latest
					versions requires reloading the page. Unsaved changes in
					the module you are currently in will be lost.
				</Typography>
				<Typography variant='body2' sx={{ mt: 2 }}>
					Reload now, or continue with the version you have loaded?
				</Typography>
			</DialogContent>
			<DialogActions>
				<Button onClick={onContinue} color='inherit'>
					Continue without reloading
				</Button>
				<Button onClick={onUpdate} variant='contained' color='primary'>
					Reload now
				</Button>
			</DialogActions>
		</Dialog>
	)
}

export default PluginUpdateDialog
