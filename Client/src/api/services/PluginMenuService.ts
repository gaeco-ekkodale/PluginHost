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
import type { PluginMenuGroupDto } from '../models/PluginMenuGroupDto';
import type { UpdatePluginLayoutRequest } from '../models/UpdatePluginLayoutRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PluginMenuService {
    /**
     * Returns the full plugin navigation menu.
     * Groups are sorted by Order; plugins within each group are also sorted by Order. Plugin URLs are pre-signed and can be used directly by the microfrontend shell.
     * @returns PluginMenuGroupDto The ordered menu tree was returned.
     * @throws ApiError
     */
    public static getPluginMenu(): CancelablePromise<Array<PluginMenuGroupDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/plugin-menu',
            errors: {
                401: `The caller is not authenticated.`,
                403: `Forbidden`,
            },
        });
    }
    /**
     * Saves a full plugin layout snapshot.
     * Updates group names and display order, and sets each plugin's group assignment and order in a single atomic call. Groups and plugins not included in the payload are left untouched.
     * @param requestBody
     * @returns any Layout was saved.
     * @throws ApiError
     */
    public static updatePluginLayout(
        requestBody: UpdatePluginLayoutRequest,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/plugin-menu',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `The payload is invalid.`,
                401: `The caller is not authenticated.`,
                403: `The caller does not have the required admin permissions.`,
                404: `One or more plugin IDs were not found.`,
            },
        });
    }
}
