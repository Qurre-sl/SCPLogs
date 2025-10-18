import {ISender} from "#senders/ISender.ts";
import {
    ENV,
    WEBHOOK_MAX_CONTENT_LENGTH,
    WEBHOOK_REQUEST_TIMEOUT_MS,
    WEBHOOK_MAX_RETRIES,
    WEBHOOK_RETRY_DELAY_MS,
    WEBHOOK_HEARTBEAT_CHECK_INTERVAL_MS
} from "#constants.ts";

interface WebhookPayload {
    content: string;
    timestamp: string;
}

export class Sender implements ISender {
    #lastHandshake = 0;

    constructor() {
        this.#lastHandshake = Date.now();
        this.#validateUrls();

        const checkDisconnected = this.#checkDisconnected.bind(this);
        setInterval(checkDisconnected, WEBHOOK_HEARTBEAT_CHECK_INTERVAL_MS);
    }

    #validateUrls(): void {
        if (!ENV.WEBHOOK_LOGS_URL && !ENV.WEBHOOK_PLAYERS_URL) {
            throw new Error("At least one webhook URL must be configured (WEBHOOK_LOGS_URL or WEBHOOK_PLAYERS_URL)");
        }
    }

    async SendLog(message: string, _channel: string): Promise<void> {
        if (message.length <= WEBHOOK_MAX_CONTENT_LENGTH) {
            await this.#sendWebhook(ENV.WEBHOOK_LOGS_URL!, message);
            return;
        }

        const chunks = this.#splitMessageIntoChunks(message);
        for (const chunk of chunks) {
            await this.#sendWebhook(ENV.WEBHOOK_LOGS_URL!, chunk);
        }
    }

    async Reply(_message: string, _original: string): Promise<void> {
        // Webhooks don't support replies
    }

    async UpdateOnline(value: string): Promise<void> {
        this.UpdateHandShake();

        if (ENV.WEBHOOK_PLAYERS_URL) {
            await this.#sendWebhook(ENV.WEBHOOK_PLAYERS_URL, value);
        }
    }

    UpdateHandShake(): void {
        this.#lastHandshake = Date.now();
    }

    #splitMessageIntoChunks(message: string): string[] {
        const chunks: string[] = [];
        const lines = message.split('\n');
        let currentChunk = '';

        for (const line of lines) {
            const lineWithBreak = line + '\n';

            if (lineWithBreak.length > WEBHOOK_MAX_CONTENT_LENGTH) {
                if (currentChunk) {
                    chunks.push(currentChunk.trimEnd());
                    currentChunk = '';
                }
                const pattern = new RegExp(`.{1,${WEBHOOK_MAX_CONTENT_LENGTH}}`, 'g');
                chunks.push(...lineWithBreak.match(pattern) ?? []);
                continue;
            }

            if (currentChunk.length + lineWithBreak.length > WEBHOOK_MAX_CONTENT_LENGTH) {
                chunks.push(currentChunk.trimEnd());
                currentChunk = lineWithBreak;
            } else {
                currentChunk += lineWithBreak;
            }
        }

        if (currentChunk) {
            chunks.push(currentChunk.trimEnd());
        }

        return chunks;
    }

    async #sendWebhook(url: string, content: string, retries = 0): Promise<void> {
        const payload: WebhookPayload = {
            content,
            timestamp: new Date().toISOString()
        };

        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), WEBHOOK_REQUEST_TIMEOUT_MS);

        try {
            const response = await fetch(url, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(payload),
                signal: controller.signal,
            });

            if (!response.ok) {
                throw new Error(`Webhook request failed: ${response.status} ${response.statusText}`);
            }
        } catch (error) {
            if (retries < WEBHOOK_MAX_RETRIES) {
                console.warn(`Webhook request failed, retrying (${retries + 1}/${WEBHOOK_MAX_RETRIES})...`);
                await this.#delay(WEBHOOK_RETRY_DELAY_MS * (retries + 1));
                return this.#sendWebhook(url, content, retries + 1);
            }

            console.error(`Failed to send webhook after ${WEBHOOK_MAX_RETRIES} retries:`, error);
            throw error;
        } finally {
            clearTimeout(timeoutId);
        }
    }

    #delay(ms: number): Promise<void> {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    async #checkDisconnected(): Promise<void> {
        if (this.#lastHandshake === 0) {
            return;
        }

        const timeoutMs = ENV.SENDER_TIMEOUT_SECONDS * 1000;
        if (this.#lastHandshake + timeoutMs > Date.now()) {
            return;
        }

        this.#lastHandshake = 0;

        if (ENV.WEBHOOK_WARN_URL) {
            const warningMessage = `⚠️ ${ENV.TRANSLATION_DISCONNECTED}`;
            await this.#sendWebhook(ENV.WEBHOOK_WARN_URL, warningMessage).catch(console.error);
        }
    }
}
