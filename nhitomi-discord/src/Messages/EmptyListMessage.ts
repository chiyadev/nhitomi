import { InteractiveMessage } from "../Interactive/message";
import { MessageContext } from "../context";

export class EmptyListMessage extends InteractiveMessage {
  constructor(context: MessageContext) {
    super(context);
    this.isStatic = true;
  }

  protected async render() {
    return {
      embed: {
        title: this.context.locale.get("list.empty.title"),
        description: this.context.locale.get("list.empty.message"),
        color: "AQUA",
      },
    };
  }
}
