import Fastify, {FastifyInstance, FastifyReply, FastifyRequest} from 'fastify';
import config from "../../configs/socket";
import sender from "../../sender/main";
import ISocket from "../ISocket";
import Command from "./Command";

class Server implements ISocket {
    commandsArray: Command[];
    fastify: FastifyInstance;

    constructor() {
        this.commandsArray = [];

        this.fastify = Fastify({
            logger: false
        })

        this.fastify.register(require('@fastify/formbody'))

        this.registerGetCommands()
        this.registerSendLog()
        this.registerReply()
        this.registerUpdateOnline()
        this.registerUpdateHandShake()

        this.fastify.listen({
            host: config.host,
            port: config.port,
        }, (err, address) => {
            if (err) {
                this.fastify.log.error(err)
                process.exit(1)
            }

            console.log('Listening: ' + address)
        })
    }

    SendCommand(command: string, original: string, author: string) {
        this.commandsArray.push({command: command, reply: original, author: author});
    }

    private checkAuth(req: FastifyRequest, reply: FastifyReply): boolean {
        if (req.headers.authorization != 'Bearer ' + config.token) {
            reply.status(401).send({ message: 'Unauthorized' });
            return false;
        }

        return true;
    }

    private registerGetCommands() {
        this.fastify.get('/Commands', (req, reply) => {
            if (!this.checkAuth(req, reply)) {
                return;
            }

            reply.status(200).send(this.commandsArray);
            this.commandsArray = [];
        })
    }

    private registerSendLog() {
        // ..body = messages: [ { text: string, channels: string[] } ]
        this.fastify.post('/SendLog', async (req, reply) => {
            if (!this.checkAuth(req, reply)) {
                return;
            }

            const body: any = req.body;

            if (typeof body.messages != 'string') {
                reply.status(400);
                return {message: '"messages" is not an string-object'};
            }

            try {
                body.messages = JSON.parse(body.messages);
            } catch {
                reply.status(400);
                return {message: '"messages" is not an object'};
            }

            let channels: any = {};

            for (let message of body.messages) {

                for (let channel of message.channels) {

                    if (channels[channel] && typeof channels[channel] == 'string') {
                        channels[channel] += '\n' + message.text;
                    } else {
                        channels[channel] = message.text;
                    }

                }

            }

            for (let data in channels) {
                await sender.SendLog(channels[data], data);
            }

            reply.status(200);
            return {message: 'OK'};
        })
    }

    private registerReply() {
        // body: { data: string, source: string }
        this.fastify.post('/Reply', async (req, reply) => {
            if (!this.checkAuth(req, reply)) {
                return;
            }

            const body: any = req.body;

            if (typeof body.data != 'string') {
                reply.status(400);
                return { message: '"data" is not string' };
            }

            if (typeof body.source != 'string') {
                reply.status(400);
                return { message: '"original" is not string' };
            }

            await sender.Reply(body.data, body.source);

            reply.status(200);
            return { message: 'OK' };
        })
    }

    private registerUpdateOnline() {
        // body: { data: string }
        this.fastify.post('/UpdateOnline', async (req, reply) => {
            if (!this.checkAuth(req, reply)) {
                return;
            }

            const body: any = req.body;

            if (typeof body.data != 'string') {
                reply.status(400);
                return { message: '"data" is not string' };
            }

            await sender.UpdateOnline(body.data);

            reply.status(200);
            return { message: 'OK' };
        })
    }

    private registerUpdateHandShake() {
        this.fastify.get('/UpdateHandShake', (req, reply) => {
            if (!this.checkAuth(req, reply)) {
                return;
            }

            sender.UpdateHandShake();

            reply.status(200);
            return { message: 'OK' };
        })
    }
}

export default Server;