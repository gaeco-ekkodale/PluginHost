// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Routes, Route } from 'react-router-dom'
import Mainpage from '../pages/Mainpage'

export default function ShellRoutes() {
	return (
		<Routes>
			<Route path='/*' element={<Mainpage />} />
			{/* Fallback für nicht gefundene Routen */}
			<Route path='*' element={<div>404 - Not Found</div>} />
		</Routes>
	)
}
