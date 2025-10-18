import {ISocket} from "#sockets/ISocket.ts";
import sender from "#senders/index.ts";
import {ENV} from "#constants.ts";

interface Command {
    command: string;
    reply: string;
    author: string;
}

interface LogMessage {
    text: string;
    channels: string[];
}

interface SocketRequest {
    token?: string;
    action?: string;
    messages?: LogMessage[];
    data?: string;
    source?: string;
}

export abstract class BaseSocket implements ISocket {
    protected commandsArray: Command[] = [];
    protected readonly RECONNECT_DELAY_MS = 5000;

    SendCommand(command: string, original: string, author: string): void {
        this.commandsArray.push({command, reply: original, author});
    }

    protected checkAuth(token?: string): boolean {
        return token === ENV.SOCKET_TOKEN;
    }

    protected async handleAction(request: SocketRequest): Promise<{error?: string; commands?: Command[]; message?: string}> {
        if (!this.checkAuth(request.token)) {
            return {error: "Unauthorized"};
        }

        switch (request.action) {
            case "GetCommands": {
                const commands = [...this.commandsArray];
                this.commandsArray = [];
                return {commands};
            }

            case "SendLog": {
                if (!Array.isArray(request.messages)) {
                    return {error: '"messages" must be an array'};
                }

                const channels: Record<string, string> = {};

                for (const msg of request.messages) {
                    for (const channel of msg.channels) {
                        if (channels[channel]) {
                            channels[channel] += "\n" + msg.text;
                        } else {
                            channels[channel] = msg.text;
                        }
                    }
                }

                for (const [channel, text] of Object.entries(channels)) {
                    await sender.SendLog(text, channel);
                }

                return {message: "OK"};
            }

            case "Reply": {
                if (typeof request.data !== "string" || typeof request.source !== "string") {
                    return {error: "Invalid parameters"};
                }

                await sender.Reply(request.data, request.source);
                return {message: "OK"};
            }

            case "UpdateOnline": {
                if (typeof request.data !== "string") {
                    return {error: '"data" must be string'};
                }

                await sender.UpdateOnline(request.data);
                return {message: "OK"};
            }

            case "UpdateHandShake": {
                sender.UpdateHandShake();
                return {message: "OK"};
            }

            default:
                return {error: "Unknown action"};
        }
    }

    protected async sleep(ms: number): Promise<void> {
        await new Promise(resolve => setTimeout(resolve, ms));
    }
}
