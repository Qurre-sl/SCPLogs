export interface ISocket {
    SendCommand(command: string, original: string, author: string): void;
}