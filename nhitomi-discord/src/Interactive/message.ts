import { Message, MessageEmbed, MessageEmbedOptions } from "discord.js-light";
import { Gauge, Histogram } from "prom-client";
import { getBuckets } from "../metrics";
import { Lock } from "semaphore-async-await";
import config from "config";
import { MessageContext } from "../context";
import deepEqual from "fast-deep-equal";
import { truncateEmbed } from "../message";
import { InteractiveInput } from "./input";
import { ReactionTrigger } from "./trigger";

const interactiveCount = new Gauge({
  name: "discord_interactives",
  help: "Number of active interactive messages.",
});

const interactiveRenderTime = new Histogram({
  name: "discord_interactive_render",
  help: "Time spent on rendering interactive messages.",
  buckets: getBuckets(0.05, 2, 6),
  labelNames: ["type"],
});

export const ActiveInteractives = new Map<string, InteractiveMessage>();

export type RenderResult = {
  message?: string;
  embed?: MessageEmbedOptions;
};

export abstract class InteractiveMessage {
  readonly lock: Lock = new Lock();
  readonly timeout = setTimeout(() => this.destroy(true), config.get<number>("interactive.timeout") * 1000);

  output?: Message;
  triggers: ReactionTrigger[] = [];

  protected constructor(readonly context: MessageContext) {
    context.ref();
  }

  protected isStatic = false;
  private lastResult?: RenderResult;

  async update() {
    this.timeout.refresh();
    await this.lock.wait();

    const measure = interactiveRenderTime.startTimer({
      type: this.constructor.name,
    });

    try {
      const result = await this.render();

      if (!result.message && !result.embed) {
        return false;
      }

      const lastOutput = this.output;

      if (this.output?.editable) {
        if (deepEqual(result, this.lastResult)) {
          console.debug("skipping render for interactive", this.constructor.name, this.output.id);
          return false;
        }

        this.output = await this.output.edit(
          result.message,
          new MessageEmbed(result.embed ? truncateEmbed(result.embed) : undefined)
        );
      } else {
        this.output = await this.context.reply(result.message, result.embed);
      }

      if (lastOutput) {
        ActiveInteractives.delete(lastOutput.id);
      }

      if (this.output) {
        ActiveInteractives.set(this.output.id, this);
        console.debug("rendered interactive", this.constructor.name, this.output.id);

        if (this.output.id !== lastOutput?.id) {
          const triggers = (this.triggers = this.createTriggers());
          const message = this.output;

          // attach triggers outside lock
          setTimeout(async () => {
            for (const trigger of triggers)
              try {
                await message.react(trigger.emoji);
              } catch {
                // ignored
              }
          }, 0);
        }
      }

      this.lastResult = result;
      return true;
    } finally {
      this.lock.signal();

      measure();
      interactiveCount.set(ActiveInteractives.size);

      if (this.isStatic) {
        setTimeout(() => this.destroy(true), 0);
      }
    }
  }

  protected createTriggers(): ReactionTrigger[] {
    return [];
  }

  protected abstract render(): Promise<RenderResult>;

  readonly ownedInputs = new Set<InteractiveInput>();

  async waitInput(content: string, timeout?: number) {
    const input = new InteractiveInput(this.context);
    this.ownedInputs.add(input);

    try {
      return await input.send(content, timeout);
    } finally {
      this.ownedInputs.delete(input);
    }
  }

  async destroy(expiring = false) {
    // reject pending inputs before entering lock to prevent a deadlock
    for (const input of this.ownedInputs) {
      input.reject();
    }

    await this.lock.wait();

    try {
      this.context && this.context.destroy();

      if (this.output) {
        console.debug("destroying interactive", this.constructor.name, this.output.id, "expiring", expiring || false);
        ActiveInteractives.delete(this.output.id);

        try {
          if (!expiring && this.output.deletable) {
            await this.output.delete();
          }
        } catch {
          // ignored
        }

        this.output = undefined;
      }

      this.lastResult = undefined;
      this.triggers = [];
    } finally {
      this.lock.signal();
      interactiveCount.set(ActiveInteractives.size);
    }
  }
}
