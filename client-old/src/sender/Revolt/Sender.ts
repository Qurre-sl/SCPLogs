import {Client} from "revolt.js";
import IClient from "../IClient";
import socket from "../../socket/main";
import config from "../../configs/sender";
import translations from "../../configs/translations";

class Sender implements IClient {
    lastHandShake: number;
    client: Client;

    constructor() {
        this.lastHandShake = 1;
        this.client = new Client();

        this.client.once('ready', () => {
            console.info(`Logged in Revolt as ${this.client.user?.username}`);
            this.client.user?.edit({status: {presence: 'Idle', text: translations.loading}});
        });

        this.client.on('messageCreate', (message) => {
            if (!message.channel) {
                return;
            }

            if (!config.allowedChannels.includes(message.channel.id)) {
                return;
            }

            if (!message.mentionIds?.includes(this.client.user?.id ?? '')) {
                return;
            }

            const command = message.content.replace(/\<@([a-zA-Z0-9]+?)\>/, '').trim();
            const reply = message.id + ':' + message.channelId;
            const author = message.author?.username ?? 'ERROR';
            socket.SendCommand(command, reply, author);
        });

        this.client.loginBot(config.auth).catch(console.error);

        let CheckDisconnected = this.CheckDisconnected.bind(this);
        setInterval(CheckDisconnected, 30000);
    }

    async SendLog(message: string, channelId: string) {
        const channel = this.client.channels.get(channelId);
        await channel?.sendMessage(message);
    }

    async Reply(message: string, original: string) {
        const originalArray = original.split(':');
        const reply = this.client.messages.get(originalArray[0]);

        if (reply) {
            await reply.reply(message);
        } else {
            const channel = this.client.channels.get(originalArray[1]);
            channel?.sendMessage(message);
        }
    }

    async UpdateOnline(value: string) {
        this.UpdateHandShake();
        this.client.user?.edit({status: {presence: 'Focus', text: value}});
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

        try {
            this.lastHandShake = 0;
            await this.client.user?.edit({status: {presence: 'Busy', text: translations.disconnected}});
        } catch {
            // rate limit handler
        }
    }
}

export default Sender;