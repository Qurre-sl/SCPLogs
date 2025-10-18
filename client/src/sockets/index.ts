import {ISocket} from "#sockets/ISocket.ts";
import {ENV} from "#constants.ts";

const socket = await createSocket();
export default socket;

async function createSocket(): Promise<ISocket> {
    switch (ENV.SOCKET_TYPE) {
        case "http": {
            const http = await import("./http/index.ts");
            return http.create();
        }
        case "tcp": {
            const tcp = await import("./tcp/index.ts");
            return tcp.create();
        }
        case "udp": {
            const udp = await import("./udp/index.ts");
            return udp.create();
        }
        case "websocket": {
            const websocket = await import("./websocket/index.ts");
            return websocket.create();
        }
        default: {
            throw new Error(`Unimplemented socket type: ${ENV.SOCKET_TYPE}`);
        }
    }
}
