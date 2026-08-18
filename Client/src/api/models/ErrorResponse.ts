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
/**
 * the dto used to send an error response to the client
 */
export type ErrorResponse = {
    /**
     * the http status code sent to the client. default is 400.
     */
    statusCode?: number;
    /**
     * the message for the error response
     */
    message?: string;
    /**
     * the collection of errors for the current context
     */
    errors?: Record<string, Array<string>>;
};

