import { captureException, init } from "@sentry/node";
import { Client, MessageReaction, PartialUser, User } from "discord.js-light";
import { collectDefaultMetrics, Counter, Gauge, register } from "prom-client";
import { rotateStatus } from "./status";
import config from "config";
import { handleCommand } from "./command";
import { handleLinks } from "./link";
import { handleInteractiveDelete, handleInteractiveInput, handleInteractiveReaction } from "./interactive";

init({
  release: `nhitomi-discord@${process.env.NODE_APP_VERSION || "Latest"}`,
  environment: process.env.NODE_ENV,
});

collectDefaultMetrics({ register });

export const Discord = new Client({
  cacheGuilds: true, // required for prometheus guild metrics
  cacheChannels: false,
  cacheOverwrites: false,
  cacheRoles: false,
  cacheEmojis: false,
  cachePresences: false,

  fetchAllMembers: false,
  messageCacheMaxSize: 0,

  ws: {
    intents: ["GUILDS", "GUILD_MESSAGES", "GUILD_MESSAGE_REACTIONS", "DIRECT_MESSAGES", "DIRECT_MESSAGE_REACTIONS"],
    large_threshold: 1,
  },
});

Discord.on("debug", console.debug);
Discord.on("warn", console.warn);
Discord.on("error", console.error);

Discord.on("ready", rotateStatus);
Discord.on("ready", () => register.setDefaultLabels({ shard: Discord.shard?.ids[0]?.toString() }));

const guildCount = new Gauge({
  name: "discord_guilds",
  help: "Number of guilds invited to.",
});

const updateGuildsMetric = () => guildCount.set(Discord.guilds.cache.size);

Discord.on("ready", updateGuildsMetric);
Discord.on("guildCreate", updateGuildsMetric);
Discord.on("guildDelete", updateGuildsMetric);

const messageCount = new Counter({
  name: "discord_messages",
  help: "Number of messages received.",
});

Discord.on("message", async (message) => {
  try {
    if (message.channel.type !== "text" && message.channel.type !== "dm") {
      return;
    }

    messageCount.inc();

    if (message.author.bot || message.author.system) {
      return;
    }

    (await handleCommand(message)) || (await handleInteractiveInput(message)) || (await handleLinks(message));
  } catch (e) {
    console.warn("unhandled error in message handler", e);
    captureException(e);
  }
});

Discord.on("messageDelete", async (message) => {
  try {
    await handleInteractiveDelete(message);
  } catch (e) {
    console.warn("unhandled error in messageDelete handler", e);
    captureException(e);
  }
});

const handleMessageReaction = async (reaction: MessageReaction, user: User | PartialUser) => {
  try {
    await handleInteractiveReaction(reaction, user);
  } catch (e) {
    console.warn("unhandled error in messageReaction handler", e);
    captureException(e);
  }
};

Discord.on("messageReactionAdd", handleMessageReaction);
Discord.on("messageReactionRemove", handleMessageReaction);

(async () => {
  while (true) {
    try {
      await Discord.login(config.get("token"));
      break;
    } catch (e) {
      console.error("could not start discord client", e);
      await new Promise((resolve) => setTimeout(resolve, 30000));
    }
  }
})();
