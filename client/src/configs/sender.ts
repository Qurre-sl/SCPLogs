import ClientType from "../sender/ClientType";

interface IConfigSender {
    type: ClientType;
    auth: string;
    allowedChannels: string[];
    timeoutSeconds: number;
}

const config : IConfigSender = {
    type: ClientType.Discord,
    auth: 'TOKEN HERE',
    allowedChannels: [ '123' ],
    timeoutSeconds: 60,
}

export default config;
