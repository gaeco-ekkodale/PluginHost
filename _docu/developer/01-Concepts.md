# Concepts

This document describes the main concepts used in the PluginHost application.

## Micro-Frontends

The application is designed as a micro-frontend architecture. The `PluginHost` acts as a shell that can load and display different plugins (micro-frontends). Each plugin is a self-contained application that can be developed and deployed independently.

## Plugin Management

The `PluginHost` is responsible for managing the plugins. This includes:

- **Loading plugins**: The `PluginHost` loads the plugins from minio.
- **Displaying plugins**: The `PluginHost` displays the plugins in the user interface.

## Authentication and Authorization

Authentication and authorization are handled by Keycloak. The `PluginHost` authenticates the user and then requests an access token specifically for a plugin by making a token exchange with the user token. The plugins can then use this token to authorize the user with its own backend.
