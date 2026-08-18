# syntax=docker/dockerfile:1.7

# Build stage
FROM node:22-alpine AS builder

WORKDIR /app

# Copy package files first to maximize layer cache hits
COPY package.json package-lock.json ./

# Install dependencies
RUN --mount=type=cache,target=/root/.npm npm ci --no-audit --no-fund

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Production stage
FROM nginx:1.27-alpine AS runtime

# Copy built assets from builder
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy nginx configuration
COPY --from=builder /app/_docker/nginx.conf /etc/nginx/conf.d/default.conf

# Copy the env substitution script and schema
COPY --from=builder /app/_docker/docker-entrypoint.sh /docker-entrypoint.sh
COPY --from=builder /app/env.d.ts /app/env.d.ts
RUN chmod +x /docker-entrypoint.sh

# Expose port
EXPOSE 80

# Use the entrypoint script to inject environment variables at runtime
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
