import {Collection, SlashCommandBuilder, SlashCommandOptionsOnlyBuilder} from "discord.js";
import config from "../../configs/sender";
import translations from "../../configs/translations";
import Socket from "../../socket/main";

class Command {
    cachedCommands: Collection<string, any>;
    data: SlashCommandOptionsOnlyBuilder;

    constructor() {
        this.cachedCommands = new Collection();
        this.data = new SlashCommandBuilder()
            .setName('execute')
            .setDescription(translations.discord.command_desc)
            .addStringOption(option =>
                option
                    .setName('command')
                    .setDescription(translations.discord.argument_desc)
                    .setRequired(true)
            )
    }

    async execute(interaction: any) {
        if (!config.allowedChannels.includes(interaction.channelId)) {
            interaction.reply({ content: translations.discord.channel_not_allowed });
            return;
        }

        const command = interaction.options.getString('command');

        if (typeof command != 'string' || command.length < 1) {
            interaction.reply({ content: translations.discord.argument_not_specified });
            return;
        }

        interaction.deferReply();

        const uid = GenerateRandom();
        this.cachedCommands.set(uid, interaction);
        Socket.SendCommand(command, uid, interaction.user.globalName)
    }

    async postExecute(message: string, original: string) {
        const interaction = this.cachedCommands.get(original);

        if (!interaction) return;

        interaction.editReply({ content: message }).catch(console.error);
        this.cachedCommands.delete(original);
    }

    getData()  {
        return this.data;
    }
}

export default Command;

function GenerateRandom() {
    return 'xxxxxxxxxxxx'.replace(/[x]/g, () => (Math.random() * 32 | 0).toString(32));
}