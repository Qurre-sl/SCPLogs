import {ENV} from "#constants.ts";
import {BaseSocket} from "#sockets/base.ts";

export class TcpSocket extends BaseSocket {
    private listener?: Deno.Listener;

    constructor() {
        super();
        this.startServer().catch(console.error);
    }

    private async startServer() {
        while (true) {
            try {
                this.listener = Deno.listen({
                    hostname: ENV.SOCKET_HOST,
                    port: ENV.SOCKET_PORT,
                    transport: "tcp",
                });

                console.log(`TCP socket listening on ${ENV.SOCKET_HOST}:${ENV.SOCKET_PORT}`);

                for await (const conn of this.listener) {
                    this.handleConnection(conn).catch(console.error);
                }

                console.error("TCP listener stopped unexpectedly, restarting in 5s...");
            } catch (error) {
                console.error("TCP server error:", error, "- restarting in 5s...");
                try {
                    this.listener?.close();
                } catch {
                    // ignore
                }
            }

            await this.sleep(this.RECONNECT_DELAY_MS);
        }
    }

    private async handleConnection(conn: Deno.Conn) {
        try {
            const buffer = new Uint8Array(65536);
            const n = await conn.read(buffer);

            if (n === null) {
                conn.close();
                return;
            }

            const data = new TextDecoder().decode(buffer.subarray(0, n));
            const request = JSON.parse(data);
            const result = await this.handleAction(request);

            await conn.write(new TextEncoder().encode(JSON.stringify(result)));
        } catch (error) {
            console.error("TCP connection error:", error);
        } finally {
            try {
                conn.close();
            } catch {
                // ignore
            }
        }
    }
}
