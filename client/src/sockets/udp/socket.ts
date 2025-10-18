import {ENV} from "#constants.ts";
import {BaseSocket} from "#sockets/base.ts";

export class UdpSocket extends BaseSocket {
    private listener?: Deno.DatagramConn;

    constructor() {
        super();
        this.startServer().catch(console.error);
    }

    private async startServer() {
        while (true) {
            try {
                this.listener = Deno.listenDatagram({
                    hostname: ENV.SOCKET_HOST,
                    port: ENV.SOCKET_PORT,
                    transport: "udp",
                });

                console.log(`UDP socket listening on ${ENV.SOCKET_HOST}:${ENV.SOCKET_PORT}`);

                const buffer = new Uint8Array(65536);

                while (true) {
                    try {
                        const [receivedData, addr] = await this.listener.receive(buffer);
                        const data = new TextDecoder().decode(receivedData);
                        const request = JSON.parse(data);
                        const result = await this.handleAction(request);

                        await this.listener.send(
                            new TextEncoder().encode(JSON.stringify(result)),
                            addr
                        );
                    } catch (error) {
                        console.error("UDP receive error:", error);
                        break;
                    }
                }

                console.error("UDP listener stopped, restarting in 5s...");
            } catch (error) {
                console.error("UDP server error:", error, "- restarting in 5s...");
                try {
                    this.listener?.close();
                } catch {
                    // ignore
                }
            }

            await this.sleep(this.RECONNECT_DELAY_MS);
        }
    }
}
