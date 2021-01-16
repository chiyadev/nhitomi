import { Counter } from "prom-client";
import { InteractiveMessage } from "./message";

const reactionTriggerInvokeCount = new Counter({
  name: "discord_interactive_trigger_invocations",
  help: "Number of times interactive reaction triggers were invoked.",
  labelNames: ["type"],
});

export abstract class ReactionTrigger {
  protected constructor(readonly interactive: InteractiveMessage, readonly emoji: string) {}

  get context() {
    return this.interactive.context;
  }

  async invoke() {
    let result: boolean;

    await this.interactive.lock.wait();

    try {
      if (!this.interactive.output) {
        return false;
      }

      console.debug(
        "invoking trigger",
        this.emoji,
        "for interactive",
        this.interactive.constructor.name,
        this.interactive.output.id
      );

      reactionTriggerInvokeCount.inc({ type: this.constructor.name });

      result = await this.run();
    } finally {
      this.interactive.lock.signal();
    }

    if (result) {
      result = await this.interactive.update();
    }

    return result;
  }

  protected abstract run(): Promise<boolean>;
}
