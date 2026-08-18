// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { GetPluginDto } from '../../../api'

/**
 * Extract sorted list of plugin IDs
 */
export const getPluginIdList = (
	plugins: GetPluginDto[] | undefined
): string[] => {
	return (plugins?.map(p => p.id) ?? []).sort()
}

/**
 * Check if two plugin lists are equal
 */
export const arePluginListsEqual = (
	newPlugins: GetPluginDto[] | undefined,
	oldPlugins: GetPluginDto[]
): boolean => {
	const newIds = getPluginIdList(newPlugins)
	const oldIds = getPluginIdList(oldPlugins)

	return (
		newIds.length === oldIds.length &&
		newIds.every((value, index) => value === oldIds[index])
	)
}
