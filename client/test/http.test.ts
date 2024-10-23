import Server from '../src/socket/Http/Server';
import socketConfigs from "../src/configs/socket";

jest.mock("../src/sender/main", () => {
    return {
        async SendLog(message: string, channels: string) {
            console.log('SendLog hooked. Message: ' + message + ', Channel: ' + channels);
        },

        async Reply(message: string, original: string) {
            console.log('Reply hooked. Message: ' + message + ', Original: ' + original);
        },

        async UpdateOnline(value: string) {
            console.log('UpdateOnline hooked. value: ' + value);
        },

        UpdateHandShake() { },
    }
})

describe("HTTP Socket", () => {
    let server: Server;


    beforeAll(() => {
        server = new Server();
    })

    afterAll(() => {
        server.fastify.close();
    })

    beforeEach(() => {
        jest.clearAllMocks();
    })

    describe("GET /Commands", () => {
        it("can i get a list of commands", async() => {
            const response = await server.fastify.inject({
                method: 'GET',
                url: '/Commands',
                headers: {
                    Authorization: `Bearer ${socketConfigs.token}`,
                }
            })

            expect(response.statusCode).toBe(200);
        });

        it("can i add and get command", async() => {
            server.SendCommand("test", "reply:id", "zxc kaneki 1000-7");

            const response = await server.fastify.inject({
                method: 'GET',
                url: '/Commands',
                headers: {
                    Authorization: `Bearer ${socketConfigs.token}`,
                }
            })

            expect(response.statusCode).toBe(200);

            expect(response.headers['content-type']).toBe('application/json; charset=utf-8');

            expect(response.json()).toStrictEqual([{
                "author": "zxc kaneki 1000-7",
                "command":"test",
                "reply":"reply:id"
            }]);
        });
    });
})