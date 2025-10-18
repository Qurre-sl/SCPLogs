import {ISocket} from "#sockets/ISocket.ts";
import {UdpSocket} from "./socket.ts";

export function create(): ISocket {
    return new UdpSocket();
}
