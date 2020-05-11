import { Message, MessageReaction } from 'discord.js'

export async function shouldHandleMessage(message: Message): Promise<boolean> {
  return !message.author.bot && !message.author.system
}

export async function shouldHandleReaction(reaction: MessageReaction): Promise<boolean> {
  return !reaction.me
}
