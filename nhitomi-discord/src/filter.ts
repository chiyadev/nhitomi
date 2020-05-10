import { Message } from 'discord.js'

export async function shouldHandleMessage(message: Message): Promise<boolean> {
  return !message.author.bot && !message.author.system
}
