import {z} from "zod";
import validate from "./validate.ts";

export const ENV = {
    SENDER_TYPE: validate(z.enum(["discord", "telegram", "webhook"]), Deno.env.get('SENDER_TYPE')),

    SENDER_TIMEOUT_SECONDS: validate(z.number().optional(), Deno.env.get('SENDER_TIMEOUT_SECONDS')) || 60,

    DISCORD_TOKEN: validate(z.string().optional(), Deno.env.get('DISCORD_TOKEN')),
    DISCORD_COMMAND_GUILDS: validate(z.array(z.bigint()).optional(), Deno.env.get('DISCORD_COMMAND_GUILDS')?.split(',').map(x => BigInt(x))) || [],
    DISCORD_ALLOWED_CHANNELS: validate(z.array(z.bigint()).optional(), Deno.env.get('DISCORD_ALLOWED_CHANNELS')?.split(',').map(x => BigInt(x))) || [],

    WEBHOOK_LOGS_URL: validate(z.string().optional(), Deno.env.get('WEBHOOK_LOGS_URL')),
    WEBHOOK_PLAYERS_URL: validate(z.string().optional(), Deno.env.get('WEBHOOK_PLAYERS_URL')),
    WEBHOOK_WARN_URL: validate(z.string().optional(), Deno.env.get('WEBHOOK_WARN_URL')),

    SOCKET_TYPE: validate(z.enum(["http", "tcp", "udp", "websocket"]), Deno.env.get('SOCKET_TYPE')),
    SOCKET_HOST: validate(z.string().optional(), Deno.env.get('SOCKET_HOST')) || "127.0.0.1",
    SOCKET_PORT: validate(z.number().optional(), parseInt(Deno.env.get('SOCKET_PORT') || "8080")) || 8080,
    SOCKET_TOKEN: validate(z.string(), Deno.env.get('SOCKET_TOKEN')),

    TRANSLATION_LOADING: validate(z.string().optional(), Deno.env.get('TRANSLATION_LOADING')) || "Loading...",
    TRANSLATION_DISCONNECTED: validate(z.string().optional(), Deno.env.get('TRANSLATION_DISCONNECTED')) || "Server disconnected",
    TRANSLATION_CHANNEL_NOT_ALLOWED: validate(z.string().optional(), Deno.env.get('TRANSLATION_CHANNEL_NOT_ALLOWED')) || "Channel not allowed",
    TRANSLATION_ARGUMENT_NOT_SPECIFIED: validate(z.string().optional(), Deno.env.get('TRANSLATION_ARGUMENT_NOT_SPECIFIED')) || "Command argument not specified",
}

// Discord Constants
export const DISCORD_MAX_MESSAGE_LENGTH = 2000;
export const DISCORD_MESSAGE_FLAG_EPHEMERAL = 1 << 6;
export const DISCORD_HEARTBEAT_CHECK_INTERVAL_MS = 30000;

// Webhook Constants
export const WEBHOOK_MAX_CONTENT_LENGTH = 2000;
export const WEBHOOK_REQUEST_TIMEOUT_MS = 5000;
export const WEBHOOK_MAX_RETRIES = 3;
export const WEBHOOK_RETRY_DELAY_MS = 1000;
export const WEBHOOK_HEARTBEAT_CHECK_INTERVAL_MS = 30000;

