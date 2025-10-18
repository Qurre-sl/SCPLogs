import {ISender} from "#senders/ISender.ts";
import {Sender} from "./sender.ts";

export function create(): ISender {
    return new Sender();
}
