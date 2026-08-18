// ============================================================================
// Environment Variables
// ============================================================================
// Add new variables here.

// Zentrale Definition aller erlaubten Environment-Variablen mit Default-Werten.
// Variablen ohne Default-Wert (null) müssen über docker-compose.yml gesetzt werden.
export const ENV_SCHEMA = {
	VITE_HOST_API_URL: null,
	VITE_KEYCLOAK_URL: null,
	VITE_KEYCLOAK_REALM: 'gaeco',
	VITE_KEYCLOAK_CLIENT_ID: 'plugin-host-client',
	VITE_SIGNALR_PATH: '/hub',
	VITE_SIGNALR_PLUGIN_EVENT: null,
	VITE_SIGNALR_OPERATION_ADD_PLUGIN: null,
} as const

// ============================================================================
// Auto-generated TypeScript Types (Do not modify below this line)
// ============================================================================

export const ENV_KEYS = Object.keys(ENV_SCHEMA) as Array<
	keyof typeof ENV_SCHEMA
>

type GeneratedEnv = {
	readonly [K in keyof typeof ENV_SCHEMA]: string
}

declare global {
	interface ImportMetaEnv extends GeneratedEnv {}
	interface ImportMeta {
		readonly env: ImportMetaEnv
	}
}

export {}
