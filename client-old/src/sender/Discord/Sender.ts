import {Client, GatewayIntentBits, ActivityType, Collection, REST, Routes} from "discord.js";
import IClient from "../IClient";
import config from "../../configs/sender";
import Command from "./Command";
import translations from "../../configs/translations";

class Sender implements IClient {
    lastHandShake: number;
    client: Client;
    commands: Collection<string, Command>;

    constructor() {
        this.lastHandShake = 1;
        this.client = new Client({intents: [GatewayIntentBits.Guilds]});
        this.commands = new Collection();

        const command = new Command();
        this.commands.set(command.getData().name, command);

        this.client.once('ready', () => {
            console.info(`Logged in Discord as ${this.client.user?.username}`);

            this.setupRest(command);

            this.client.user?.setPresence({
                activities: [{name: translations.loading, type: ActivityType.Custom}],
                status: 'idle'
            });
        });

        this.client.on('interactionCreate', async interaction => {
            if (!interaction.isChatInputCommand())
                return;

            const command = this.commands.get(interaction.commandName);

            if (!command)
                return;

            command.execute(interaction).catch(() => interaction.reply({
                content: translations.discord.command_error,
                ephemeral: true
            }).catch(console.error))
        });

        this.client.login(config.auth).catch(console.error);

        let CheckDisconnected = this.CheckDisconnected.bind(this);
        setInterval(CheckDisconnected, 30000);
    }

    private setupRest(command: Command) {
        const rest = new REST({version: '10'}).setToken(config.auth);
        rest.put(
            Routes.applicationCommands(this.client.user?.id ?? ''), {
                body: [command.getData().toJSON()]
            },
        ).catch(console.error);
    }

    async SendLog(message: string, channelId: string) {
        const channel: any = await this.client.channels.fetch(channelId);

        if (!channel) return;

        if (message.length < 2000) {
            await channel.send({content: message});
            return;
        }

        const messages = message.split('\n');
        let cached = '';

        for (const part of messages) {
            if (cached.length + part.length > 2000) {
                await channel.send({content: cached});
                cached = '';
            }

            cached += part + '\n';
        }

        await channel.send({content: cached});
    }

    async Reply(message: string, original: string) {
        this.commands.get('execute')?.postExecute(message, original);
    }

    async UpdateOnline(value: string) {
        this.UpdateHandShake();
        this.client.user?.setPresence({
            activities: [{name: value, type: ActivityType.Custom}],
            status: 'online'
        });
    }

    UpdateHandShake() {
        this.lastHandShake = Date.now();
    }

    private async CheckDisconnected() {
        if (this.lastHandShake == 0) {
            return;
        }

        if (this.lastHandShake + (config.timeoutSeconds * 1000) > Date.now()) {
            return;
        }

        this.lastHandShake = 0;
        this.client.user?.setPresence({
            activities: [{name: translations.disconnected, type: ActivityType.Custom}],
            status: 'dnd'
        });
    }
}

export default Sender;