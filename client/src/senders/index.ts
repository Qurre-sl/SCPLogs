import {ISender} from "#senders/ISender.ts";
import {ENV} from "#constants.ts";

const sender = await createSender();
export default sender;

async function createSender(): Promise<ISender> {
    switch (ENV.SENDER_TYPE) {
        case "discord": {
            const discord = await import("./discord/index.ts");
            return discord.create();
        }
        case "webhook": {
            const webhook = await import("./webhook/index.ts");
            return webhook.create();
        }
        default: {
            throw new Error(`Unimplemented type: ${ENV.SENDER_TYPE}`);
        }
    }
}