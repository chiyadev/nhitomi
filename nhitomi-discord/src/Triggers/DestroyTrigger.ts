import { ReactionTrigger } from "../Interactive/trigger";
import { InteractiveMessage } from "../Interactive/message";

export class DestroyTrigger extends ReactionTrigger {
  constructor(interactive: InteractiveMessage) {
    super(interactive, "\uD83D\uDDD1");
  }

  protected async run(): Promise<boolean> {
    // setTimeout avoids deadlock
    setTimeout(() => this.interactive.destroy(), 0);
    return false;
  }
}
