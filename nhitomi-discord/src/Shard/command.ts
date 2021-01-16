import { Counter, Histogram } from "prom-client";
import { getBuckets } from "../metrics";
import config from "config";
import { Message } from "discord.js-light";
import { MessageContext } from "../context";
import { captureException } from "@sentry/node";

export type CommandFunc = (context: MessageContext, arg?: string) => Promise<boolean>;

export async function getCommand(command: string): Promise<CommandFunc | undefined> {
  try {
    const module = await import(`../Commands/${command}`);

    if ("run" in module) {
      return module.run;
    }
  } catch {
    // ignored
  }
}

const commandTime = new Histogram({
  name: "discord_command",
  help: "Time spent on executing commands.",
  buckets: getBuckets(0.05, 10, 12),
  labelNames: ["command"],
});

const commandErrorCount = new Counter({
  name: "discord_command_errors",
  help: "Number of errors while executing commands.",
  labelNames: ["command"],
});

const prefix = config.get<string>("prefix");

export async function handleCommand(message: Message) {
  if (!message.content.startsWith(prefix)) {
    return false;
  }

  const content = message.content.substring(prefix.length).trim();
  const space = content.indexOf(" ");
  const command = space === -1 ? content : content.substring(0, space);
  const arg = space === -1 ? undefined : content.substring(space + 1).trim();

  const run = await getCommand(command);

  if (!run) {
    return true;
  }

  await message.fetch();

  if (message.channel.type === "text") {
    await message.channel.fetch();
  }

  const measure = commandTime.startTimer({ command });
  const context = await MessageContext.create(message);

  try {
    // ensure nsfw channel
    if (message.channel.type === "text" && !message.channel.nsfw) {
      await context.reply(context.locale.get("nsfw.notAllowed"));
    } else {
      console.debug(
        `user ${context.user.id} '${context.user.username}' executing command '${command}' with args '${arg || ""}'`
      );

      await run(context, arg);
    }
  } catch (e) {
    captureException(e);

    await context.reply("", {
      title: context.locale.get("error.title"),
      color: "RED",
      description: `
${context.locale.get("error.description")}

\`\`\`
${e.stack}
\`\`\`
`.trim(),
      footer: {
        text: `${context.user.username} (${context.user.id})`,
      },
    });

    commandErrorCount.inc({ command });
  } finally {
    context.destroy();
    measure();
  }

  return true;
}
