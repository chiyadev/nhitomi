import config from "config";
import { Discord } from ".";
import { captureException } from "@sentry/node";

export async function rotateStatus() {
  while (true) {
    const adjectives = config.get<string>("status.adjectives").split(";");
    const adjective = adjectives[Math.floor(Math.random() * adjectives.length)];

    const nouns = config.get<string>("status.nouns").split(";");
    const noun = nouns[Math.floor(Math.random() * nouns.length)];

    // replace adjective and noun with random selection
    let text = config.get<string>("status.format").replace("adjective", adjective).replace("noun", noun);

    // add help command hint
    text = `${text} [${config.get<string>("prefix")}help]`;

    try {
      await Discord.user?.setPresence({
        activity: {
          name: text,
          type: "PLAYING",
        },
      });
    } catch (e) {
      console.debug("could not update presence", e);
      captureException(e);
    }

    await new Promise((resolve) => setTimeout(resolve, config.get<number>("status.interval") * 1000));
  }
}
