import {ENV} from "#constants.ts";
import {BaseSocket} from "#sockets/base.ts";

export class WebSocketSocket extends BaseSocket {
    private server?: Deno.HttpServer;

    constructor() {
        super();
        this.startServer().catch(console.error);
    }

    private async startServer() {
        while (true) {
            try {
                const handler = (req: Request): Response => {
                    const upgrade = req.headers.get("upgrade") || "";

                    if (upgrade.toLowerCase() !== "websocket") {
                        return new Response("Expected WebSocket upgrade", {status: 426});
                    }

                    const {socket, response} = Deno.upgradeWebSocket(req);

                    socket.onopen = () => console.log("WebSocket client connected");

                    socket.onmessage = async (event) => {
                        try {
                            const request = JSON.parse(event.data);
                            const result = await this.handleAction(request);
                            socket.send(JSON.stringify(result));
                        } catch (error) {
                            console.error("WebSocket message error:", error);
                            socket.send(JSON.stringify({error: "Internal error"}));
                        }
                    };

                    socket.onerror = (error) => console.error("WebSocket error:", error);
                    socket.onclose = () => console.log("WebSocket client disconnected");

                    return response;
                };

                this.server = Deno.serve({
                    hostname: ENV.SOCKET_HOST,
                    port: ENV.SOCKET_PORT,
                    handler,
                    onListen: () => {
                        console.log(`WebSocket listening on ${ENV.SOCKET_HOST}:${ENV.SOCKET_PORT}`);
                    },
                });

                await this.server.finished;
                console.error("WebSocket server stopped unexpectedly, restarting in 5s...");
            } catch (error) {
                console.error("WebSocket server error:", error, "- restarting in 5s...");
            }

            await this.sleep(this.RECONNECT_DELAY_MS);
        }
    }
}
