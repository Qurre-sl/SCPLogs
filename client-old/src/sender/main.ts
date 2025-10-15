import config from "../configs/sender";
import ClientType from "./ClientType";
import IClient from "./IClient";
import Discord from "./Discord/Sender";
import Revolt from "./Revolt/Sender";
import Telegram from "./Telegram/Sender";
import WebHook from "./WebHook/Sender";

let client: IClient;

switch (config.type) {
    case ClientType.Discord:
        client = new Discord();
        break;

    case ClientType.Revolt:
        client = new Revolt();
        break;

    case ClientType.Telegram:
        client = new Telegram();
        break;

    case ClientType.WebHook:
        client = new WebHook();
        break;

    default: {
        throw new Error(`Unknown sender type ${config.type}`);
    }
}

export default client;