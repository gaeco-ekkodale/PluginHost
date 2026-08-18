// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/**
 * Fallback start page, shown when no homepage plugin is registered.
 *
 * Deliberately minimal: the getting-started checklist lives in the Homepage
 * micro-frontend, which the host renders at "/" as soon as it is available.
 */
const RootContent = () => {
	return (
		<div className='flex w-full grow flex-col items-center justify-center'>
			<h1 className='text-3xl font-bold'>Welcome to gaeco</h1>
			<h2 className='pt-4 text-xl'>
				Please choose an application from the menu in the top right to get
				started.
			</h2>
		</div>
	)
}

export default RootContent
