import { Message, MessageReaction, PartialMessage, PartialUser, User } from "discord.js-light";
import { ActiveInteractives } from "../Interactive/message";
import { ActiveInputs } from "../Interactive/input";

export async function handleInteractiveInput(message: Message) {
  for (const input of ActiveInputs) {
    const inputMessage = input.context.message;

    if (inputMessage.author.id === message.author.id && inputMessage.channel.id === message.channel.id) {
      input.resolve(message);
      return true;
    }
  }

  return false;
}

export async function handleInteractiveDelete(message: Message | PartialMessage) {
  const interactive = ActiveInteractives.get(message.id);

  if (message.id === interactive?.output?.id) {
    await interactive.destroy(true);
    return true;
  }

  return false;
}

export async function handleInteractiveReaction(reaction: MessageReaction, user: User | PartialUser) {
  const interactive = ActiveInteractives.get(reaction.message.id);

  if (
    reaction.message.id === interactive?.output?.id &&
    user.id === interactive.context.message.author.id &&
    !interactive.ownedInputs.size
  ) {
    const trigger = interactive.triggers.find(({ emoji }) => emoji === reaction.emoji.name);

    if (trigger) {
      return await trigger.invoke();
    }
  }

  return false;
}
