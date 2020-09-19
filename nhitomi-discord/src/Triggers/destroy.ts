import { ReactionTrigger } from "../interactive";

export class DestroyTrigger extends ReactionTrigger {
  readonly emoji = "\uD83D\uDDD1";

  protected async run(): Promise<boolean> {
    const interactive = this.interactive;

    if (!interactive) return false;

    // schedule destroy to avoid deadlock (triggers are automatically locked)
    setTimeout(() => interactive.destroy(), 0);
    return true;
  }
}
