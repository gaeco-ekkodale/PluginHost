// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import React, { useEffect, useRef, useState } from 'react'
import { Divider, IconButton, Tooltip } from '@mui/material'
import DragIndicatorIcon from '@mui/icons-material/DragIndicator'
import DeleteIcon from '@mui/icons-material/Delete'
import {
	SortableContext,
	rectSortingStrategy,
	useSortable,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import type { DraftGroup } from './types'
import SortablePlugin from './SortablePlugin'

interface Props {
	sortId: string
	group: DraftGroup
	gi: number
	onRename: (groupId: string, newName: string) => void
	onDelete: (groupId: string) => void
}

/**
 * Sortable group container used in app-menu edit mode.
 */
const SortableGroup: React.FC<Props> = ({
	sortId,
	group,
	gi,
	onRename,
	onDelete,
}) => {
	const {
		attributes,
		listeners,
		setNodeRef,
		transform,
		transition,
		isDragging,
	} = useSortable({ id: sortId })
	const pluginSortIds = group.plugins.map(p => `p:${group.groupId}:${p.id}`)
	const [editingName, setEditingName] = useState(false)
	const [nameValue, setNameValue] = useState(group.name)
	const inputRef = useRef<HTMLInputElement>(null)

	useEffect(() => {
		if (editingName) inputRef.current?.focus()
	}, [editingName])

	useEffect(() => {
		setNameValue(group.name)
	}, [group.name])

	/**
	 * Commits a renamed group title if it contains a meaningful change.
	 */
	const commitRename = () => {
		setEditingName(false)
		const trimmed = nameValue.trim()
		if (trimmed && trimmed !== group.name) onRename(group.groupId, trimmed)
		else setNameValue(group.name)
	}

	return (
		<div
			ref={setNodeRef}
			style={{
				transform: CSS.Transform.toString(transform),
				transition,
				opacity: isDragging ? 0.3 : 1,
			}}
		>
			{gi > 0 && <Divider />}
			<div className='flex items-center gap-1 p-1 text-gray-400'>
				<span className='cursor-grab' {...attributes} {...listeners}>
					<DragIndicatorIcon fontSize='small' />
				</span>
				{editingName ? (
					<input
						ref={inputRef}
						value={nameValue}
						onChange={e => setNameValue(e.target.value)}
						onBlur={commitRename}
						onKeyDown={e => {
							if (e.key === 'Enter') commitRename()
							if (e.key === 'Escape') {
								setNameValue(group.name)
								setEditingName(false)
							}
						}}
						className='flex-1 rounded border border-gray-300 px-1 py-0.5 text-xs font-semibold tracking-wide text-gray-500 uppercase outline-none focus:border-blue-400'
					/>
				) : (
					<Tooltip title='Umbenennen'>
						<span
							className='flex-1 cursor-pointer text-xs font-semibold tracking-wide uppercase hover:text-gray-600'
							onClick={() => setEditingName(true)}
						>
							{group.name || 'Group'}
						</span>
					</Tooltip>
				)}
				<Tooltip
					title={
						group.plugins.length === 0
							? 'Delete group'
							: 'Move all apps out of this group first'
					}
				>
					<span>
						<IconButton
							size='small'
							onClick={() => onDelete(group.groupId)}
							disabled={group.plugins.length > 0}
						>
							<DeleteIcon fontSize='small' />
						</IconButton>
					</span>
				</Tooltip>
			</div>
			<SortableContext
				items={pluginSortIds}
				strategy={rectSortingStrategy}
			>
				<div className='grid min-h-16 min-w-90 grid-cols-3 gap-2 p-2'>
					{group.plugins.length === 0 ? (
						<div className='col-span-3 flex items-center justify-center rounded-lg border-2 border-dashed border-gray-200 py-3 text-xs text-gray-400'>
							Apps hierher ziehen
						</div>
					) : (
						group.plugins.map(plugin => (
							<SortablePlugin
								key={plugin.id}
								sortId={`p:${group.groupId}:${plugin.id}`}
								plugin={plugin}
							/>
						))
					)}
				</div>
			</SortableContext>
		</div>
	)
}

export default SortableGroup
