import IClient from "../IClient";

class Sender implements IClient {
    constructor() {

    }

    async SendLog(message: string, channel: string) {
    }

    async Reply(message: string, original: string) {
    }

    async UpdateOnline(value: string) {
    }

    UpdateHandShake() {
    }
}

export default Sender;