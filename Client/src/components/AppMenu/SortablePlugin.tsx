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
import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import pluginFallbackIcon from '../../assets/plugin-icon.svg'
import type { DraftPlugin } from './types'

const loadedIconUrls = new Set<string>()
const failedIconUrls = new Set<string>()

interface Props {
	sortId: string
	plugin: DraftPlugin
}

/**
 * Sortable plugin tile rendered inside edit-mode groups.
 */
const SortablePlugin: React.FC<Props> = ({ sortId, plugin }) => {
	const primaryIcon = plugin.iconUrl || ''
	const hasPrimaryIcon = Boolean(primaryIcon)
	const [iconSrc, setIconSrc] = useState(
		hasPrimaryIcon && !failedIconUrls.has(primaryIcon)
			? primaryIcon
			: pluginFallbackIcon
	)
	const [isImageReady, setIsImageReady] = useState(
		!hasPrimaryIcon ||
			loadedIconUrls.has(primaryIcon) ||
			failedIconUrls.has(primaryIcon)
	)
	const showFallbackPlaceholder =
		hasPrimaryIcon && iconSrc === primaryIcon && !isImageReady

	useEffect(() => {
		if (!hasPrimaryIcon) {
			setIconSrc(pluginFallbackIcon)
			setIsImageReady(true)
			return
		}

		if (failedIconUrls.has(primaryIcon)) {
			setIconSrc(pluginFallbackIcon)
			setIsImageReady(true)
			return
		}

		setIconSrc(primaryIcon)
		setIsImageReady(loadedIconUrls.has(primaryIcon))
	}, [hasPrimaryIcon, primaryIcon])

	const {
		attributes,
		listeners,
		setNodeRef,
		transform,
		transition,
		isDragging,
	} = useSortable({ id: sortId })

	return (
		<div
			ref={setNodeRef}
			style={{
				transform: CSS.Transform.toString(transform),
				transition,
				opacity: isDragging ? 0.3 : 1,
				cursor: 'grab',
			}}
			className='flex min-h-24 min-w-26 flex-col items-center justify-center gap-2 rounded-lg p-2 text-gray-600 select-none hover:bg-gray-200'
			{...attributes}
			{...listeners}
		>
			{showFallbackPlaceholder && (
				<img
					src={pluginFallbackIcon}
					alt=''
					aria-hidden='true'
					className='absolute h-10 w-10 object-contain'
					style={{ pointerEvents: 'none' }}
				/>
			)}
			<img
				src={iconSrc}
				alt={plugin.name}
				className={`h-10 w-10 object-contain transition-opacity duration-150 ${isImageReady ? 'opacity-100' : 'opacity-0'}`}
				style={{ pointerEvents: 'none' }}
				onLoad={() => {
					if (primaryIcon) loadedIconUrls.add(primaryIcon)
					setIsImageReady(true)
				}}
				onError={() => {
					if (primaryIcon) failedIconUrls.add(primaryIcon)
					setIconSrc(pluginFallbackIcon)
					setIsImageReady(true)
				}}
			/>
			<span
				className='text-sm font-medium'
				style={{ pointerEvents: 'none' }}
			>
				{plugin.name}
			</span>
		</div>
	)
}

export default SortablePlugin
