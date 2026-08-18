// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import type { PluginMenuGroupDto } from '../../api'

/**
 * Lightweight plugin shape used by the app-menu UI.
 */
export type DraftPlugin = {
	id: string
	name: string
	iconUrl: string
	route: string
}

/**
 * Group shape used for both view-mode and edit-mode rendering.
 */
export type DraftGroup = {
	groupId: string
	name: string
	plugins: DraftPlugin[]
	isNew?: boolean
}

/**
 * Converts API menu groups to visible UI draft groups.
 */
export function toVisibleDraft(groups: PluginMenuGroupDto[]): DraftGroup[] {
	return groups
		.map(g => ({
			groupId: g.groupId ?? '',
			name: g.name ?? '',
			plugins: (g.plugins ?? [])
				.filter(p => !p.id.toLowerCase().includes('homepage'))
				.map(p => ({
					id: p.id,
					name: p.name,
					iconUrl: p.iconUrl,
					route: p.route,
				})),
		}))
		.filter(g => g.plugins.length > 0)
}
