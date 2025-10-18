import {
    type CreateSlashApplicationCommand,
    InteractionResponseTypes,
    ApplicationCommandOptionTypes
} from "@discordeno/types";
import {ENV, DISCORD_MESSAGE_FLAG_EPHEMERAL} from "#constants.ts";
import type {CustomBot, CustomInteraction} from "./sender.ts";
import Socket from "#sockets/index.ts";

const cachedInteractions = new Map<string, CustomInteraction>();

export default {
    command: {
        name: "ex",
        description: "Execute a command",
        descriptionLocalizations: {
            ru: "Выполнить команду"
        },
        options: [{
            type: ApplicationCommandOptionTypes.String,
            name: "command",
            nameLocalizations: {
                ru: "команда"
            },
            description: "Enter your command here",
            descriptionLocalizations: {
                ru: "Введите здесь команду"
            },
            required: true
        }]
    } as CreateSlashApplicationCommand,

    async execute(bot: CustomBot, interaction: CustomInteraction) {
        if (interaction.data?.name !== this.command.name) {
            return;
        }

        if (!interaction.channelId || !ENV.DISCORD_ALLOWED_CHANNELS.includes(interaction.channelId)) {
            await bot.helpers.sendInteractionResponse(
                interaction.id,
                interaction.token,
                {
                    type: InteractionResponseTypes.ChannelMessageWithSource,
                    data: {
                        content: ENV.TRANSLATION_CHANNEL_NOT_ALLOWED,
                        flags: DISCORD_MESSAGE_FLAG_EPHEMERAL
                    },
                }
            );
            return;
        }

        const commandOption = interaction.data?.options?.find(opt => opt.name === "command");
        const command = commandOption?.value as string | undefined;

        if (typeof command !== 'string' || command.length < 1) {
            await bot.helpers.sendInteractionResponse(
                interaction.id,
                interaction.token,
                {
                    type: InteractionResponseTypes.ChannelMessageWithSource,
                    data: {
                        content: ENV.TRANSLATION_ARGUMENT_NOT_SPECIFIED,
                        flags: DISCORD_MESSAGE_FLAG_EPHEMERAL
                    },
                }
            );
            return;
        }

        // Defer reply to indicate processing
        await bot.helpers.sendInteractionResponse(
            interaction.id,
            interaction.token,
            {
                type: InteractionResponseTypes.DeferredChannelMessageWithSource,
            }
        );

        const uid = crypto.randomUUID();
        cachedInteractions.set(uid, interaction);

        Socket.SendCommand(command, uid, interaction.user?.username || "Unknown")
    },

    async postExecute(bot: CustomBot, message: string, original: string) {
        const interaction = cachedInteractions.get(original);

        if (!interaction) return;

        await bot.helpers.editOriginalInteractionResponse(
            interaction.token,
            {
                content: message
            }
        ).catch(console.error);

        cachedInteractions.delete(original);
    }
}