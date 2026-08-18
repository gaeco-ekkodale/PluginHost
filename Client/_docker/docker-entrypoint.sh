#!/bin/sh
# Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
#
# This file is part of the gaeco platform system.
#
# Use of this file is governed by the terms of the license
# in LICENSE.md at the root of this repository.
# Unauthorized copying, modification, distribution, or use of this file,
# via any medium, is strictly prohibited except as expressly permitted
# under that license.

# Replace environment variables in all JavaScript files
# This allows runtime configuration of the built application

echo "Validating required environment variables..."

# Extract required variables (those with null values) from env.d.ts
REQUIRED_VARS=$(grep -E '^  VITE_[A-Z_]+: null,' /app/env.d.ts | sed -E 's/^  ([A-Z_]+):.*/\1/' || true)

# Check if all required variables are set
MISSING_VARS=""
for var in $REQUIRED_VARS; do
    eval "value=\${$var}"
    if [ -z "$value" ]; then
        MISSING_VARS="$MISSING_VARS $var"
    fi
done

if [ -n "$MISSING_VARS" ]; then
    echo "ERROR: Required environment variables are not set:$MISSING_VARS"
    echo "Please set these variables in your docker-compose.yml or environment."
    exit 1
fi

echo "All required environment variables are set."
echo "Injecting runtime environment variables..."

# Get all environment variables that start with VITE_
env | grep '^VITE_' | while IFS='=' read -r name value; do
    placeholder="${name}_PLACEHOLDER"
    
    # If value is empty, check if there's a default in env.d.ts
    if [ -z "$value" ]; then
        default=$(grep -E "^  ${name}:" /app/env.d.ts | sed -E 's/.*: "(.+)",$/\1/' || true)
        if [ -n "$default" ] && [ "$default" != "null" ]; then
            value="$default"
            echo "Using default value for ${name}"
        fi
    fi
    
    echo "Replacing ${placeholder} with value from ${name}"
    
    # Escape special characters in the value for sed
    escaped_value=$(echo "$value" | sed 's/[&/\]/\\&/g')
    
    # Find all JS files and replace the placeholder
    find /usr/share/nginx/html -type f -name "*.js" -exec sed -i \
      "s|${placeholder}|${escaped_value}|g" {} \;
done

echo "Environment variables injected successfully"

# Execute the CMD
exec "$@"
