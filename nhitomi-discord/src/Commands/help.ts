import { HelpMessage } from "../Messages/HelpMessage";
import { CommandFunc } from "../Shard/command";

export const run: CommandFunc = (context) => new HelpMessage(context).update();
