// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { IconButton, Menu } from '@mui/material'
import React from 'react'
import { PluginMenuGroupDto } from '../api'
import { Link, useLocation } from 'react-router-dom'
import AppsIcon from '@mui/icons-material/Apps'
import Logo from '../assets/gaeco_logo_horizontal_white.svg'
import LogoutMenu from './LogoutMenu'
import PluginMenuContent from './AppMenu/PluginMenuContent'

interface NavbarProps {
	menuGroups: PluginMenuGroupDto[]
	hasLoaded: boolean
	loadFailed: boolean
	onLayoutSaved: () => void
	onReload: () => void
}

const Navbar = ({
	menuGroups,
	hasLoaded,
	loadFailed,
	onLayoutSaved,
	onReload,
}: NavbarProps) => {
	const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null)
	const currentPath = useLocation().pathname

	const handleClose = () => setAnchorEl(null)
	const handleMenu = (event: React.MouseEvent<HTMLElement>) =>
		setAnchorEl(event.currentTarget)
	const isSelected = (pluginRoute: string) =>
		currentPath.startsWith(pluginRoute)

	return (
		<nav
			className='bg-primary text-light sticky top-0 h-16 w-full'
			role='navigation'
		>
			<div className='flex h-full items-center justify-between'>
				<div className='flex items-center gap-4 p-2'>
					<Link to='/' className='flex items-center gap-2'>
						<img
							src={Logo}
							alt='Logo'
							className='h-10 w-auto pl-2'
						/>
					</Link>
				</div>
				<div className='flex items-center p-2'>
					<IconButton onClick={handleMenu}>
						<AppsIcon sx={{ color: 'Background' }} />
					</IconButton>
					<Menu
						id='menu-appbar'
						anchorEl={anchorEl}
						keepMounted
						transformOrigin={{
							vertical: 'top',
							horizontal: 'right',
						}}
						open={Boolean(anchorEl)}
						onClose={handleClose}
						className='flex items-center justify-center shadow-lg'
					>
						<PluginMenuContent
							menuGroups={menuGroups}
							hasLoaded={hasLoaded}
							loadFailed={loadFailed}
							isOpen={Boolean(anchorEl)}
							onClose={handleClose}
							onLayoutSaved={onLayoutSaved}
							onReload={onReload}
							isSelected={isSelected}
						/>
					</Menu>
					<LogoutMenu />
				</div>
			</div>
		</nav>
	)
}

export default Navbar
