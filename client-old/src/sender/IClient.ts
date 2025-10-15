interface IClient {
    SendLog(message: string, channel: string): Promise<void>;
    Reply(message: string, original: string): Promise<void>;
    UpdateOnline(value: string): Promise<void>;
    UpdateHandShake(): void;
}

export default IClient;