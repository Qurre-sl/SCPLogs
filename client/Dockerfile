# Stage 1: Bundle
FROM denoland/deno:alpine AS builder

WORKDIR /app

# Copy source
COPY . .

# Compile to standalone binary
RUN deno compile \
    --allow-all \
    --output scplogs \
    src/main.ts

# Stage 2: Runtime
FROM alpine:latest

WORKDIR /app

# Copy compiled binary
COPY --from=builder /app/scplogs .

ENTRYPOINT ["./scplogs"]