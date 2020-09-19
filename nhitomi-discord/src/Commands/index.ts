import { MessageContext } from "../context";
import { promisify } from "util";
import fs from "fs";
import path from "path";

export type CommandFunc = (context: MessageContext, arg?: string) => Promise<boolean>
export type CommandModule = { name: string, run: CommandFunc }

export const modules: CommandModule[] = [];

export async function loadCommands(): Promise<void> {
  modules.length = 0;

  for (const x of await promisify(fs.readdir)("Commands")) {
    const xp = path.parse(x);

    if (xp.ext !== ".js")
      continue;

    try {
      const module = await import(`./${xp.name}`);

      if ("run" in module) {
        modules.push({
          name: xp.name,
          run: module.run
        });
      }
    } catch (e) {
      console.warn("could not import command module", x, module);
    }
  }
}

export function matchCommand(command: string): CommandModule | undefined {
  return modules.find(m => m.name.startsWith(command.toLowerCase()));
}
