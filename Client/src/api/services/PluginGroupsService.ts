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
import type { AddPluginGroupRequest } from '../models/AddPluginGroupRequest';
import type { GetPluginGroupDto } from '../models/GetPluginGroupDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PluginGroupsService {
    /**
     * Creates a new plugin group.
     * @param requestBody
     * @returns any The group was created.
     * @throws ApiError
     */
    public static addPluginGroup(
        requestBody: AddPluginGroupRequest,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/plugin-groups',
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
     * Fetches all plugin groups.
     * @returns GetPluginGroupDto All groups were returned.
     * @throws ApiError
     */
    public static getAllPluginGroups(): CancelablePromise<Array<GetPluginGroupDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/plugin-groups',
            errors: {
                401: `The caller is not authenticated.`,
                403: `The caller does not have the required admin permissions.`,
                404: `No groups were found.`,
            },
        });
    }
    /**
     * Deletes the plugin group with the specified ID.
     * @param groupId
     * @returns any The group was deleted.
     * @throws ApiError
     */
    public static deletePluginGroup(
        groupId: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/plugin-groups/{groupId}',
            path: {
                'groupId': groupId,
            },
            errors: {
                401: `The caller is not authenticated.`,
                403: `The caller does not have the required admin permissions.`,
            },
        });
    }
}
