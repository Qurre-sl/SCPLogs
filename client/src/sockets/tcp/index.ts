import {ISocket} from "#sockets/ISocket.ts";
import {TcpSocket} from "./socket.ts";

export function create(): ISocket {
    return new TcpSocket();
}
