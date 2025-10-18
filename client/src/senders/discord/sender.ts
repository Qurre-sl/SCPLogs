import {ISender} from "#senders/ISender.ts";
import {
    type Bot,
    createBot,
    GatewayIntents,
    type Interaction,
} from "@discordeno/bot";
import {
    ENV,
    DISCORD_MAX_MESSAGE_LENGTH,
    DISCORD_MESSAGE_FLAG_EPHEMERAL,
    DISCORD_HEARTBEAT_CHECK_INTERVAL_MS
} from "#constants.ts";
import cmd from "./command.ts";
import {InteractionResponseTypes, ActivityTypes} from "discordeno/types";
import {getDesiredProperties} from "./desiredProperties.ts";
import type {CompleteDesiredProperties, DesiredPropertiesBehavior, SetupDesiredProps} from "@discordeno/bot";

export type CustomDesiredProperties = CompleteDesiredProperties<ReturnType<typeof getDesiredProperties>>;
export type CustomBot = Bot<CustomDesiredProperties, DesiredPropertiesBehavior.RemoveKey>;
export type CustomInteraction = SetupDesiredProps<Interaction, CustomDesiredProperties, DesiredPropertiesBehavior.RemoveKey>;

export class Sender implements ISender {
    #lastHandshake = 0;
    readonly #client: CustomBot;

    constructor() {
        this.#lastHandshake = 1;

        this.#client = createBot({
            token: ENV.DISCORD_TOKEN!,
            intents: GatewayIntents.Guilds,
            desiredProperties: getDesiredProperties(),
            events: {
                ready: async (payload) => {
                    console.info(`🟢 Bot online: ${payload.user.username}#${payload.user.discriminator}`);
                    await this.#setStatus("idle", ENV.TRANSLATION_LOADING);
                },
                interactionCreate: (interaction) => {
                    cmd.execute(this.#client, interaction)
                        .catch((err) => {
                            this.#handleInteractionError(interaction, err);
                            throw err;
                        });
                }
            },
        });

        this.#init().catch(console.error);

        const checkDisconnected = this.#checkDisconnected.bind(this);
        setInterval(checkDisconnected, DISCORD_HEARTBEAT_CHECK_INTERVAL_MS);
    }

    async #init() {
        for (const guildId of ENV.DISCORD_COMMAND_GUILDS) {
            await this.#client.rest.upsertGuildApplicationCommands(guildId, [
                cmd.command
            ]);
        }

        await this.#client.start();
    }

    async SendLog(message: string, channelId: string): Promise<void> {
        const channel = await this.#client.helpers.getChannel(channelId);
        if (!channel) return;

        if (message.length <= DISCORD_MAX_MESSAGE_LENGTH) {
            await this.#client.helpers.sendMessage(channelId, { content: message });
            return;
        }

        const chunks = this.#splitMessageIntoChunks(message);
        for (const chunk of chunks) {
            await this.#client.helpers.sendMessage(channelId, { content: chunk });
        }
    }

    #splitMessageIntoChunks(message: string): string[] {
        const chunks: string[] = [];
        const lines = message.split('\n');
        let currentChunk = '';

        for (const line of lines) {
            const lineWithBreak = line + '\n';

            if (lineWithBreak.length > DISCORD_MAX_MESSAGE_LENGTH) {
                if (currentChunk) {
                    chunks.push(currentChunk.trimEnd());
                    currentChunk = '';
                }
                const pattern = new RegExp(`.{1,${DISCORD_MAX_MESSAGE_LENGTH}}`, 'g');
                chunks.push(...lineWithBreak.match(pattern) ?? []);
                continue;
            }

            if (currentChunk.length + lineWithBreak.length > DISCORD_MAX_MESSAGE_LENGTH) {
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

    async Reply(message: string, original: string): Promise<void> {
        await cmd.postExecute(this.#client, message, original);
    }

    async UpdateOnline(value: string): Promise<void> {
        this.UpdateHandShake();
        await this.#setStatus("online", value);
    }

    UpdateHandShake(): void {
        this.#lastHandshake = Date.now();
    }

    #handleInteractionError(interaction: CustomInteraction, error: Error): void {
        this.#client.helpers.sendInteractionResponse(
            interaction.id,
            interaction.token,
            {
                type: InteractionResponseTypes.ChannelMessageWithSource,
                data: {
                    content: `Something went wrong: ${error.message}`,
                    flags: DISCORD_MESSAGE_FLAG_EPHEMERAL,
                },
            }
        ).catch(console.error);
    }

    async #setStatus(status: "online" | "idle" | "dnd", state: string): Promise<void> {
        await this.#client.gateway.editBotStatus({
            status,
            activities: [{
                type: ActivityTypes.Custom,
                name: "Custom Status",
                state,
            }],
        });
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
        await this.#setStatus("dnd", ENV.TRANSLATION_DISCONNECTED);
    }
}