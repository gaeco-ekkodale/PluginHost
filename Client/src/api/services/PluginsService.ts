// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CreateClientRequest } from '../models/CreateClientRequest';
import type { ExchangeTokenRequest } from '../models/ExchangeTokenRequest';
import type { GetPluginDto } from '../models/GetPluginDto';
import type { RegisterContainerPluginRequest } from '../models/RegisterContainerPluginRequest';
import type { TokenResponse } from '../models/TokenResponse';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PluginsService {
    /**
     * Creates a new client in the identity provider system.
     * Creates a new Keycloak client with token exchange capability. Used when registering new plugin clients that require their own authentication scope.
     * @param requestBody
     * @returns string Returns the ID of the created client.
     * @throws ApiError
     */
    public static createClientEndpoint(
        requestBody: CreateClientRequest,
    ): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/plugins/create-client',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
                401: `The caller is not authenticated.`,
                403: `The caller does not have the required admin permissions.`,
            },
        });
    }
    /**
     * Registers a new container plugin to the system.
     * Registers a microfrontend plugin by its metadata. The plugin is identified by the combination of PluginName and Module.
     * @param requestBody
     * @returns any The plugin was successfully registered.
     * @throws ApiError
     */
    public static createPlugin(
        requestBody: RegisterContainerPluginRequest,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/plugins',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `The request payload is invalid.`,
                401: `The caller is not authenticated.`,
                403: `The caller does not have the required admin permissions.`,
            },
        });
    }
    /**
     * Retrieves the plugin index.
     * @returns GetPluginDto Returns the list of plugins or an empty collection if none are found.
     * @throws ApiError
     */
    public static getAllPlugins(): CancelablePromise<Array<GetPluginDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/plugins',
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
            },
        });
    }
    /**
     * Deletes a plugin from the system.
     * Removes the plugin registration and all associated container files. This operation is irreversible.
     * @param pluginId
     * @returns any The plugin was successfully deleted.
     * @throws ApiError
     */
    public static deletePlugin(
        pluginId: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/plugins/{pluginId}',
            path: {
                'pluginId': pluginId,
            },
            errors: {
                401: `The caller is not authenticated.`,
                403: `The caller does not have the required admin permissions.`,
                404: `No plugin with the specified ID was found.`,
            },
        });
    }
    /**
     * Exchanges an access token for a new token valid for a specific client.
     * Allows authenticated users to exchange their current access token for one that can be used with a specific plugin client.
     * @param clientId
     * @param requestBody
     * @returns TokenResponse Returns the new token response.
     * @throws ApiError
     */
    public static exchangeTokenEndpoint(
        clientId: string,
        requestBody: ExchangeTokenRequest,
    ): CancelablePromise<TokenResponse> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/plugins/{clientId}/token',
            path: {
                'clientId': clientId,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `The access token or client ID is missing or invalid.`,
                401: `Unauthorized`,
                403: `Forbidden`,
            },
        });
    }
    /**
     * Retrieves all plugins that the current user has access to.
     * @returns GetPluginDto Returns the list of licensed plugins or an empty collection.
     * @throws ApiError
     */
    public static getMyPlugins(): CancelablePromise<Array<GetPluginDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/plugins/my-plugins',
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
            },
        });
    }
    /**
     * Retrieves a plugin file by token and file path.
     * Proxies a file from the plugin container after validating the signed URL token. No user login is required; the signed token provides scoped access.
     * @param token
     * @param filename
     * @returns any Returns the requested file with its correct content type.
     * @throws ApiError
     */
    public static getPluginFile(
        token: string,
        filename: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/plugins/{token}/{**filename}',
            path: {
                'token': token,
                '**filename': filename,
            },
            errors: {
                401: `The signed token has expired.`,
                403: `The token does not grant access to the requested file.`,
                404: `The requested file was not found in the plugin container.`,
            },
        });
    }
}
