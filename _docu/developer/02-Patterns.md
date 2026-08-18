# Patterns

This document describes the design patterns used in the PluginHost application.

## Repository Pattern

The repository pattern is used in the backend to abstract the data access layer. The `IPluginRepository` and `IPluginGroupRepository` interfaces define the methods for accessing the data, and the `PluginRepository` and `PluginGroupRepository` classes provide the implementation. This pattern allows to easily switch the database implementation without changing the business logic.

## Options Pattern

The options pattern is used to configure the application. The `MinioOptions`, `KeycloakOpenIdOptions`, and `GetPluginOptions` classes define the configuration options, and the `appsettings.json` file provides the values. This pattern allows to change the configuration without recompiling the application.
