import config from "../configs/socket";
import ISocket from "./ISocket";
import SocketType from "./SocketType";
import Http from "./Http/Server";

let socket : ISocket;

switch (config.protocol) {
    case SocketType.Http:
        socket = new Http();
        break;

    default: {
        throw new Error(`Unsupported protocol: ${config.protocol}`);
    }
}

export default socket;