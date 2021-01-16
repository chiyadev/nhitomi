import { Locale } from "./locales";
import { User } from "nhitomi-api";
import { Message, MessageEmbedOptions } from "discord.js-light";
import { ApiClient, createApiClient } from "./client";
import { truncateEmbed } from "./message";
import { InteractiveInput } from "./Interactive/input";
import config from "config";

const tokenCache = new Map<string, string>();

export class MessageContext {
  readonly locale: Locale;

  private constructor(readonly message: Message, readonly client: ApiClient, readonly user: User) {
    this.locale = Locale.get(user.language);
  }

  refCount = 1;

  /**
   * Increments the ref count of this context.
   * Ref counting is used to keep this context alive beyond the scope of the message handler (e.g. for interactive messages).
   */
  ref(): this {
    ++this.refCount;
    return this;
  }

  /** Decrements the ref count of this context, and destroys it if the count is zero. */
  destroy(): void {
    const count = --this.refCount;

    if (count === 0) {
      console.log("context destroyed for message", this.message.id);
    }
  }

  async reply(content?: string, embed?: MessageEmbedOptions) {
    if (embed) {
      return await this.message.channel.send({
        content: content || undefined,
        embed: truncateEmbed(embed),
      });
    } else if (content) {
      return await this.message.channel.send(content);
    }
  }

  async notify(content?: string, timeout?: number) {
    const message = await this.reply(content);

    setTimeout(async () => {
      if (message?.deletable) {
        try {
          await message.delete();
        } catch (e) {
          console.warn("could not delete message", message.id, e);
        }
      }
    }, timeout || config.get<number>("interactive.notificationTimeout") * 1000);
  }

  waitInput(content: string, timeout?: number) {
    return new InteractiveInput(this).send(content, timeout);
  }

  static async create(message: Message): Promise<MessageContext> {
    const cacheKey = message.author.id;
    const cachedToken = tokenCache.get(cacheKey);

    if (cachedToken) {
      try {
        const client = createApiClient(cachedToken);
        const user = await client.user.getSelfUser();

        return new MessageContext(message, client, user);
      } catch {
        tokenCache.delete(cacheKey);
      }
    }

    const { token, user } = await createApiClient().internal.getOrCreateUserDiscord({
      getOrCreateDiscordUserRequest: {
        id: message.author.id,
        username: message.author.username,
        discriminator: parseInt(message.author.discriminator),
      },
    });

    tokenCache.set(cacheKey, token);

    return new MessageContext(message, createApiClient(token), user);
  }
}
