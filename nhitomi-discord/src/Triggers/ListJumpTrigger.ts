import { ReactionTrigger } from "../Interactive/trigger";
import { ListTriggerTarget } from "./ListTrigger";
import { InteractiveMessage } from "../Interactive/message";

function getEmoji(destination: "start" | "end" | "input") {
  switch (destination) {
    case "start":
      return "\u23EA";

    case "end":
      return "\u23E9";

    case "input":
      return "\ud83d\udcd1";
  }
}

export type ListJumpTriggerTarget = ListTriggerTarget & {
  length: number;
};

export class ListJumpTrigger extends ReactionTrigger {
  constructor(
    readonly interactive: InteractiveMessage & ListJumpTriggerTarget,
    readonly destination: "start" | "end" | "input"
  ) {
    super(interactive, getEmoji(destination));
  }

  protected async run() {
    switch (this.destination) {
      case "start":
        this.interactive.position = 0;
        break;

      case "end":
        this.interactive.position = this.interactive.length - 1;
        break;

      case "input": {
        const result = parseInt(
          (await this.interactive.waitInput(this.context.locale.get("reaction.list.jump"))) || ""
        );

        if (isNaN(result)) {
          return false;
        }

        this.interactive.position = result - 1;
        break;
      }
    }

    return true;
  }
}
