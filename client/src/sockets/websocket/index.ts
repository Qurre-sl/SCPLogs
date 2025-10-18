import {ISocket} from "#sockets/ISocket.ts";
import {WebSocketSocket} from "./socket.ts";

export function create(): ISocket {
    return new WebSocketSocket();
}
