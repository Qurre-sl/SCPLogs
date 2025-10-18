import {ISocket} from "#sockets/ISocket.ts";
import {HttpSocket} from "./socket.ts";

export function create(): ISocket {
    return new HttpSocket();
}