# Data Model

This document describes the data model of the PluginHost application.

## Entities

### PluginEntity

The `PluginEntity` represents a plugin in the database. It has the following properties:

- `Id` (string, required): The unique identifier of the plugin.
- `PluginName` (string, required): The unique identifier of the plugin (technical name).
- `Module` (string, required): The module to which the plugin belongs.
- `VersionCode` (int, required): The version number as integer for comparisons.
- `Version` (string, required): The version as string (e.g., "1.0.0").
- `DisplayName` (string, required): The display name of the plugin.
- `Description` (string, optional): A description of the plugin.
- `Route` (string, required): The route under which the plugin is accessible.
- `IconFilename` (string, required): The filename of the plugin icon.
- `GroupId` (string, required): The foreign key to the `PluginGroup` entity.

### PluginGroup

The `PluginGroup` represents a group of plugins. It has the following properties:

- `Id` (string, required): The unique identifier and name of the plugin group.

## Data Transfer Objects (DTOs)

### PluginDto

Base DTO for plugin data:

- `DisplayName` (string, required): The display name of the plugin.
- `Description` (string, optional): A description of the plugin.
- `PluginName` (string, required): The technical name of the plugin.
- `Module` (string, required): The module of the plugin.
- `Version` (string, required): The version as string.
- `VersionCode` (int, required): The version as integer.
- `Route` (string, required): The route of the plugin.
- `GroupId` (string, required): The ID of the plugin group.

### NewPluginDto

Extends `PluginDto` for creating new plugins:

- All properties from `PluginDto`
- `Files` (IFormFileCollection, required): The plugin files to upload.
- `IconFilename` (string, required): The filename of the icon.

### GetPluginDto

Extends `PluginDto` for retrieving plugin data:

- All properties from `PluginDto`
- `Id` (string, required): The ID of the plugin.
- `Url` (string, required): The URL to the plugin.
- `IconUrl` (string, required): The URL to the plugin icon.

### GetPluginGroupDto

DTO for retrieving plugin groups:

- `Id` (string, required): The ID of the plugin group.

### PatchPluginGroupDto

DTO for updating plugin groups:

- `PluginId` (string, required): The ID of the plugin.
- `GroupName` (string, required): The name of the group.

## Additional Models

### PluginToken

Used for authentication of plugin requests:

- `PluginId` (string, required): The ID of the plugin.
- `Filename` (string, optional): The filename (optional).

### DatabaseContainer

Container class for database seeding:

- `PluginGroups` (List<PluginGroup>): List of plugin groups.
- `Plugins` (List<PluginEntity>): List of plugins.

## Relationships

- A `PluginEntity` belongs to one `PluginGroup` (via `GroupId`).
- A `PluginGroup` can have multiple `PluginEntity` instances.

This is a one-to-many relationship between `PluginGroup` and `PluginEntity`.
