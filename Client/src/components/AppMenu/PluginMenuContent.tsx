// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import React, { useEffect, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button, CircularProgress, IconButton, Tooltip } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import CloseIcon from '@mui/icons-material/Close'
import SaveIcon from '@mui/icons-material/Save'
import TuneIcon from '@mui/icons-material/Tune'
import {
	DndContext,
	DragEndEvent,
	DragOverlay,
	PointerSensor,
	closestCenter,
	useSensor,
	useSensors,
} from '@dnd-kit/core'
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable'
import { Link } from 'react-router-dom'
import { PluginMenuGroupDto, PluginMenuService } from '../../api'
import { useSnackbar } from '../../context/SnackbarProvider'
import pluginFallbackIcon from '../../assets/plugin-icon.svg'
import { DraftGroup, toVisibleDraft } from './types'
import SortableGroup from './SortableGroup'
import { applyDraftDrag } from './dndUtils'

interface Props {
	menuGroups: PluginMenuGroupDto[]
	hasLoaded: boolean
	/** The menu could not be fetched - shown instead of an empty app list. */
	loadFailed: boolean
	isOpen: boolean
	onClose: () => void
	onLayoutSaved: () => void
	onReload: () => void
	isSelected: (route: string) => boolean
}

const APP_GRID_CLASS = 'grid min-w-[360px] grid-cols-3 gap-2 p-2'
const APP_TILE_CLASS =
	'flex min-w-[104px] flex-col items-center justify-center gap-2 rounded-lg p-2 text-gray-600 transition duration-300 hover:bg-gray-200 hover:text-gray-800'

/**
 * Deep-clones visible draft groups to keep edit state isolated.
 */
const cloneDraftGroups = (groups: DraftGroup[]): DraftGroup[] =>
	groups.map(g => ({
		...g,
		plugins: [...g.plugins],
	}))

/**
 * Renders and manages the plugin app menu in view and edit modes.
 */
const PluginMenuContent: React.FC<Props> = ({
	menuGroups,
	hasLoaded,
	loadFailed,
	isOpen,
	onClose,
	onLayoutSaved,
	onReload,
	isSelected,
}) => {
	const showSnackbar = useSnackbar()
	const [editMode, setEditMode] = useState(false)
	const [displayGroups, setDisplayGroups] = useState<DraftGroup[]>(() =>
		toVisibleDraft(menuGroups)
	)
	const [draftGroups, setDraftGroups] = useState<DraftGroup[]>([])
	const [deletedGroupIds, setDeletedGroupIds] = useState<string[]>([])
	const [activeId, setActiveId] = useState<string | null>(null)
	const queryClient = useQueryClient()
	const [brokenIconPluginIds, setBrokenIconPluginIds] = useState<Set<string>>(
		() => new Set()
	)

	useEffect(() => {
		if (!isOpen) setEditMode(false)
	}, [isOpen])

	useEffect(() => {
		setDisplayGroups(toVisibleDraft(menuGroups))
		setBrokenIconPluginIds(new Set())
	}, [menuGroups])

	/**
	 * Stores plugin IDs with unreachable icons to force fallback rendering.
	 */
	const markIconAsBroken = (pluginId: string) => {
		setBrokenIconPluginIds(prev => {
			if (prev.has(pluginId)) return prev
			const next = new Set(prev)
			next.add(pluginId)
			return next
		})
	}

	/**
	 * Returns the icon source with fallback support for missing/broken icons.
	 */
	const getIconSrc = (pluginId: string, iconUrl?: string | null) =>
		brokenIconPluginIds.has(pluginId)
			? pluginFallbackIcon
			: iconUrl || pluginFallbackIcon

	const enterEditMode = () => {
		setDraftGroups(cloneDraftGroups(displayGroups))
		setDeletedGroupIds([])
		setEditMode(true)
	}

	const cancelEdit = () => {
		setEditMode(false)
		setDraftGroups([])
		setDeletedGroupIds([])
	}

	const renameGroup = (groupId: string, newName: string) =>
		setDraftGroups(prev =>
			prev.map(g => (g.groupId === groupId ? { ...g, name: newName } : g))
		)

	const deleteGroup = (groupId: string) => {
		if (!draftGroups.find(g => g.groupId === groupId)?.isNew)
			setDeletedGroupIds(prev => [...prev, groupId])
		setDraftGroups(prev => prev.filter(g => g.groupId !== groupId))
	}

	const addGroup = () =>
		setDraftGroups(prev => [
			...prev,
			{
				groupId: crypto.randomUUID(),
				name: 'New group',
				plugins: [],
				isNew: true,
			},
		])

	const sensors = useSensors(
		useSensor(PointerSensor, { activationConstraint: { distance: 6 } })
	)

	const onDragStart = ({ active }: { active: { id: string | number } }) =>
		setActiveId(String(active.id))

	const onDragEnd = ({ active, over }: DragEndEvent) => {
		setActiveId(null)
		if (!over || active.id === over.id) return
		const aId = String(active.id)
		const oId = String(over.id)

		setDraftGroups(prev => applyDraftDrag(prev, aId, oId))
	}

	const saveLayoutMutation = useMutation({
		mutationFn: () =>
			PluginMenuService.updatePluginLayout({
				groups: draftGroups.map(g => ({
					groupId: g.groupId,
					name: g.name,
					plugins: g.plugins.map(p => p.id),
				})),
				deletedGroupIds,
			}),
	})

	const handleSave = async () => {
		try {
			await saveLayoutMutation.mutateAsync()
			await queryClient.invalidateQueries({
				queryKey: ['pluginhost-plugin-menu'],
			})
			onLayoutSaved()
			setEditMode(false)
			showSnackbar('Order saved.', 'success')
		} catch {
			showSnackbar('Could not save the order.', 'error')
		}
	}

	const activePlugin = activeId?.startsWith('p:')
		? draftGroups
				.flatMap(g =>
					g.plugins.map(p => ({
						...p,
						sortId: `p:${g.groupId}:${p.id}`,
					}))
				)
				.find(p => p.sortId === activeId)
		: null
	const activeGroupName = activeId?.startsWith('g:')
		? draftGroups.find(g => `g:${g.groupId}` === activeId)?.name
		: null
	const activePluginIconSrc =
		activePlugin && getIconSrc(activePlugin.id, activePlugin.iconUrl)

	return (
		<div className='flex flex-col items-center justify-center overflow-auto'>
			{/* Header */}
			<div className='relative flex w-full items-center justify-center px-10 py-2'>
				<span className='mx-4 text-center text-lg font-semibold text-gray-700'>
					Applications
				</span>
				<div className='absolute top-1/2 right-1 -translate-y-1/2'>
					{editMode ? (
						<Tooltip title='Cancel editing'>
							<IconButton size='small' onClick={cancelEdit}>
								<CloseIcon fontSize='small' />
							</IconButton>
						</Tooltip>
					) : (
						<Tooltip title='Edit order'>
							<IconButton size='small' onClick={enterEditMode}>
								<TuneIcon fontSize='small' />
							</IconButton>
						</Tooltip>
					)}
				</div>
			</div>

			{/* Body */}
			{!hasLoaded ? (
				<div className='m-2 flex flex-col items-center gap-2'>
					<CircularProgress />
					<div>Loading…</div>
				</div>
			) : loadFailed && !editMode ? (
				<div className='m-4 flex max-w-[320px] flex-col items-center gap-2 px-4 text-center'>
					<span className='font-medium text-gray-700'>
						Applications could not be loaded
					</span>
					<span className='text-sm text-gray-500'>
						The app list is currently unavailable. Please try again
						in a moment.
					</span>
					<Button size='small' onClick={onReload}>
						Try again
					</Button>
				</div>
			) : displayGroups.length === 0 && !editMode ? (
				<div className='m-4 flex max-w-[320px] flex-col items-center gap-1 px-4 text-center'>
					<span className='font-medium text-gray-700'>
						No applications available
					</span>
					<span className='text-sm text-gray-500'>
						There are either no applications installed, or none have
						been released for your account. Please contact your
						administrator if you expect to see applications here.
					</span>
				</div>
			) : editMode ? (
				<DndContext
					sensors={sensors}
					collisionDetection={closestCenter}
					onDragStart={onDragStart}
					onDragEnd={onDragEnd}
				>
					<div className='m-2 max-h-full overflow-y-auto'>
						<SortableContext
							items={draftGroups.map(g => `g:${g.groupId}`)}
							strategy={verticalListSortingStrategy}
						>
							{draftGroups.map((group, gi) => (
								<SortableGroup
									key={group.groupId}
									sortId={`g:${group.groupId}`}
									group={group}
									gi={gi}
									onRename={renameGroup}
									onDelete={deleteGroup}
								/>
							))}
						</SortableContext>
					</div>
					<DragOverlay>
						{activePlugin && (
							<div className='flex flex-col items-center justify-center gap-2 rounded-lg bg-white p-2 shadow-xl'>
								<img
									src={activePluginIconSrc}
									alt={activePlugin.name}
									className='h-10 w-10 object-contain'
									onError={() => {
										if (activePlugin.iconUrl) {
											markIconAsBroken(activePlugin.id)
										}
									}}
								/>
								<span className='line-clamp-2 text-center text-sm leading-tight font-medium'>
									{activePlugin.name}
								</span>
							</div>
						)}
						{activeGroupName && (
							<div className='rounded bg-white px-3 py-1 text-sm font-semibold text-gray-600 shadow-lg'>
								{activeGroupName}
							</div>
						)}
					</DragOverlay>
				</DndContext>
			) : (
				<div className='m-2 grid max-h-80 grid-flow-row gap-1 divide-y-2 divide-gray-200 overflow-auto'>
					{displayGroups.map((group, gi) => (
						<div
							key={group.groupId || gi}
							className={APP_GRID_CLASS}
						>
							{group.plugins.map(plugin => {
								const iconSrc = getIconSrc(
									plugin.id,
									plugin.iconUrl
								)
								const sharedClasses = `${APP_TILE_CLASS} ${isSelected(plugin.route) ? 'bg-gray-100' : ''}`

								const content = (
									<>
										<img
											src={iconSrc}
											alt={plugin.name}
											className='h-10 w-10 object-contain'
											onError={() => {
												if (plugin.iconUrl) {
													markIconAsBroken(plugin.id)
												}
											}}
										/>
										<span className='text-sm font-medium'>
											{plugin.name}
										</span>
									</>
								)

								return (
									<Link
										key={plugin.id}
										to={plugin.route}
										onClick={onClose}
										className={sharedClasses}
									>
										{content}
									</Link>
								)
							})}
						</div>
					))}
				</div>
			)}

			{/* Footer */}
			{editMode && (
				<div className='flex w-full flex-col border-t border-gray-200'>
					<button
						onClick={addGroup}
						className='flex w-full items-center gap-1 px-4 py-2 text-sm text-gray-500 hover:bg-gray-50'
					>
						<AddIcon fontSize='small' />
						Add group
					</button>
					<div className='flex items-center justify-between border-t border-gray-100 px-4 py-2'>
						<span className='text-xs text-gray-400'>
							Drag to reorder
						</span>
						<Button
							size='small'
							variant='contained'
							onClick={handleSave}
							disabled={saveLayoutMutation.isPending}
							startIcon={
								saveLayoutMutation.isPending ? (
									<CircularProgress size={14} />
								) : (
									<SaveIcon fontSize='small' />
								)
							}
						>
							Save
						</Button>
					</div>
				</div>
			)}
		</div>
	)
}

export default PluginMenuContent
