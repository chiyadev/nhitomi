import { Message } from "discord.js-light";
import { MessageContext } from "../context";
import config from "config";

export const ActiveInputs = new Set<InteractiveInput>();

export class InteractiveInput {
  constructor(readonly context: MessageContext) {}

  async send(content: string, timeout?: number) {
    ActiveInputs.add(this);

    const promise = new Promise<Message>((resolve, reject) => {
      this.resolve = resolve;
      this.reject = reject;

      setTimeout(reject, (timeout || config.get<number>("interactive.inputTimeout")) * 1000);
    });

    const sent = await this.context.reply(content);
    let received: Message | undefined;

    try {
      received = await promise;
      return received.content;
    } catch {
      // ignored
    } finally {
      ActiveInputs.delete(this);

      try {
        sent?.deletable && (await sent.delete());
        received?.deletable && (await received.delete());
      } catch {
        // ignored
      }
    }
  }

  resolve = (message: Message | PromiseLike<Message>) => {};
  reject = (reason?: any) => {};
}
