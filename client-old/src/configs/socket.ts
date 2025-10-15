import SocketType from "../socket/SocketType";

interface IConfigSocket {
    host: string;
    port: number;
    protocol: SocketType;
    token: string;
}

const config : IConfigSocket = {
    host: '127.0.0.1',
    port: 8080,
    protocol: SocketType.Http,
    token: 'GENERATE RANDOM',
}

export default config;