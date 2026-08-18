// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { arrayMove } from '@dnd-kit/sortable'
import type { DraftGroup } from './types'

/**
 * Applies a drag-and-drop update to draft groups for group and plugin items.
 */
export const applyDraftDrag = (
	groups: DraftGroup[],
	activeSortId: string,
	overSortId: string
): DraftGroup[] => {
	if (activeSortId.startsWith('g:')) {
		return moveGroup(groups, activeSortId, overSortId)
	}

	if (activeSortId.startsWith('p:')) {
		return movePlugin(groups, activeSortId, overSortId)
	}

	return groups
}

const moveGroup = (
	groups: DraftGroup[],
	activeSortId: string,
	overSortId: string
): DraftGroup[] => {
	const targetGroupId = overSortId.startsWith('g:')
		? overSortId.slice(2)
		: overSortId.startsWith('p:')
			? overSortId.split(':')[1]
			: null

	if (!targetGroupId) return groups

	const oldIndex = groups.findIndex(g => `g:${g.groupId}` === activeSortId)
	const newIndex = groups.findIndex(g => g.groupId === targetGroupId)

	if (oldIndex < 0 || newIndex < 0) return groups

	return arrayMove(groups, oldIndex, newIndex)
}

const movePlugin = (
	groups: DraftGroup[],
	activeSortId: string,
	overSortId: string
): DraftGroup[] => {
	const sourceGroupId = activeSortId.split(':')[1]
	const targetGroupId = overSortId.startsWith('p:')
		? overSortId.split(':')[1]
		: overSortId.startsWith('g:')
			? overSortId.slice(2)
			: null

	if (!targetGroupId) return groups

	// Dropping a plugin onto its own group container (not onto another plugin item)
	// should not remove the item.
	if (sourceGroupId === targetGroupId && !overSortId.startsWith('p:')) {
		return groups
	}

	if (sourceGroupId === targetGroupId && overSortId.startsWith('p:')) {
		return groups.map(g => {
			if (g.groupId !== sourceGroupId) return g

			const oldIndex = g.plugins.findIndex(
				p => `p:${g.groupId}:${p.id}` === activeSortId
			)
			const newIndex = g.plugins.findIndex(
				p => `p:${g.groupId}:${p.id}` === overSortId
			)

			if (oldIndex < 0 || newIndex < 0) return g

			return { ...g, plugins: arrayMove(g.plugins, oldIndex, newIndex) }
		})
	}

	const plugin = groups
		.find(g => g.groupId === sourceGroupId)
		?.plugins.find(p => `p:${sourceGroupId}:${p.id}` === activeSortId)

	if (!plugin) return groups

	return groups.map(g => {
		if (g.groupId === sourceGroupId) {
			return {
				...g,
				plugins: g.plugins.filter(p => p.id !== plugin.id),
			}
		}

		if (g.groupId === targetGroupId) {
			const insertIndex = overSortId.startsWith('p:')
				? g.plugins.findIndex(
						p => `p:${g.groupId}:${p.id}` === overSortId
					)
				: g.plugins.length

			const nextPlugins = [...g.plugins]
			nextPlugins.splice(
				insertIndex < 0 ? nextPlugins.length : insertIndex,
				0,
				plugin
			)

			return { ...g, plugins: nextPlugins }
		}

		return g
	})
}
