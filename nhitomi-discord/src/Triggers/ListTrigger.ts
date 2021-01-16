import { ReactionTrigger } from "../Interactive/trigger";
import { InteractiveMessage } from "../Interactive/message";

function getEmoji(direction: "left" | "right") {
  switch (direction) {
    case "left":
      return "\u25c0";

    case "right":
      return "\u25b6";
  }
}

export type ListTriggerTarget = {
  position: number;
};

export class ListTrigger extends ReactionTrigger {
  constructor(readonly interactive: InteractiveMessage & ListTriggerTarget, readonly direction: "left" | "right") {
    super(interactive, getEmoji(direction));
  }

  protected async run() {
    switch (this.direction) {
      case "left":
        this.interactive.position--;
        break;

      case "right":
        this.interactive.position++;
        break;
    }

    return true;
  }
}
