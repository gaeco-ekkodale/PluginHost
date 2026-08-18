// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { UserManager, WebStorageStateStore } from 'oidc-client-ts'

export const appUserManager = new UserManager({
	authority:
		import.meta.env.VITE_KEYCLOAK_URL +
		'/realms/' +
		import.meta.env.VITE_KEYCLOAK_REALM,
	client_id: import.meta.env.VITE_KEYCLOAK_CLIENT_ID,
	scope: 'openid',
	redirect_uri: `${window.location.origin}${window.location.pathname}`,
	post_logout_redirect_uri: window.location.origin,
	userStore: new WebStorageStateStore({ store: window.sessionStorage }),
	monitorSession: true,
})
