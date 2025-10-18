import {ENV} from "#constants.ts";
import {BaseSocket} from "#sockets/base.ts";

interface LogMessage {
    text: string;
    channels: string[];
}

export class HttpSocket extends BaseSocket {
    private server?: Deno.HttpServer;

    constructor() {
        super();
        this.startServer();
    }

    private async startServer() {
        while (true) {
            try {
                this.server = Deno.serve({
                    hostname: ENV.SOCKET_HOST,
                    port: ENV.SOCKET_PORT,
                    handler: (req) => this.handleRequest(req),
                    onListen: () => {
                        console.log(`HTTP socket listening on ${ENV.SOCKET_HOST}:${ENV.SOCKET_PORT}`);
                    },
                });

                await this.server.finished;
                console.error("HTTP server stopped unexpectedly, restarting in 5s...");
            } catch (error) {
                console.error("HTTP server error:", error, "- restarting in 5s...");
            }

            await this.sleep(this.RECONNECT_DELAY_MS);
        }
    }

    private async handleRequest(req: Request): Promise<Response> {
        if (!this.checkAuthHeader(req)) {
            return this.jsonResponse({message: "Unauthorized"}, 401);
        }

        const url = new URL(req.url);

        try {
            switch (url.pathname) {
                case "/Commands":
                    return this.handleGetCommands();
                case "/SendLog":
                    return await this.handleSendLog(req);
                case "/Reply":
                    return await this.handleReply(req);
                case "/UpdateOnline":
                    return await this.handleUpdateOnline(req);
                case "/UpdateHandShake":
                    return this.handleUpdateHandShake();
                default:
                    return this.jsonResponse({message: "Not Found"}, 404);
            }
        } catch (error) {
            console.error("Request error:", error);
            return this.jsonResponse({message: "Internal Server Error"}, 500);
        }
    }

    private checkAuthHeader(req: Request): boolean {
        const auth = req.headers.get("Authorization");
        return auth === `Bearer ${ENV.SOCKET_TOKEN}`;
    }

    private jsonResponse(data: unknown, status = 200): Response {
        return new Response(
            JSON.stringify(data),
            {status, headers: {"Content-Type": "application/json"}}
        );
    }

    private handleGetCommands(): Response {
        const commands = [...this.commandsArray];
        this.commandsArray = [];
        return this.jsonResponse(commands);
    }

    private async handleSendLog(req: Request): Promise<Response> {
        const body = await req.formData();
        const messagesStr = body.get("messages");

        if (typeof messagesStr !== "string") {
            return this.jsonResponse({message: '"messages" is not a string'}, 400);
        }

        let messages: LogMessage[];
        try {
            messages = JSON.parse(messagesStr);
        } catch {
            return this.jsonResponse({message: '"messages" is not valid JSON'}, 400);
        }

        await this.handleAction({
            token: ENV.SOCKET_TOKEN,
            action: "SendLog",
            messages
        });

        return this.jsonResponse({message: "OK"});
    }

    private async handleReply(req: Request): Promise<Response> {
        const body = await req.formData();
        const data = body.get("data");
        const source = body.get("source");

        if (typeof data !== "string") {
            return this.jsonResponse({message: '"data" is not string'}, 400);
        }

        if (typeof source !== "string") {
            return this.jsonResponse({message: '"source" is not string'}, 400);
        }

        await this.handleAction({
            token: ENV.SOCKET_TOKEN,
            action: "Reply",
            data,
            source
        });

        return this.jsonResponse({message: "OK"});
    }

    private async handleUpdateOnline(req: Request): Promise<Response> {
        const body = await req.formData();
        const data = body.get("data");

        if (typeof data !== "string") {
            return this.jsonResponse({message: '"data" is not string'}, 400);
        }

        await this.handleAction({
            token: ENV.SOCKET_TOKEN,
            action: "UpdateOnline",
            data
        });

        return this.jsonResponse({message: "OK"});
    }

    private handleUpdateHandShake(): Response {
        this.handleAction({
            token: ENV.SOCKET_TOKEN,
            action: "UpdateHandShake"
        });

        return this.jsonResponse({message: "OK"});
    }
}
