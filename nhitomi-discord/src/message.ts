import { MessageEmbedOptions } from "discord.js-light";

function truncateStr(max: number, s?: string) {
  if (s && s.length > max) {
    return s.substring(0, max - 3) + "...";
  }

  return s;
}

// https://discordjs.guide/popular-topics/embeds.html#embed-limits
export function truncateEmbed(embed: MessageEmbedOptions) {
  embed.title = truncateStr(256, embed.title);
  embed.description = truncateStr(2048, embed.description);

  if (embed.author) {
    embed.author.name = truncateStr(256, embed.author.name);
  }

  if (embed.footer) {
    embed.footer.text = truncateStr(2048, embed.footer.text);
  }

  if (embed.fields) {
    embed.fields = embed.fields.slice(0, 25);

    for (const field of embed.fields) {
      field.name = truncateStr(256, field.name);
      field.value = truncateStr(1024, field.value);
    }
  }

  // todo: truncate to 6000 characters total limit
  return embed;
}
