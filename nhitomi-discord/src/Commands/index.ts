import { Message } from 'discord.js'

export type CommandFunc = (message: Message, arg?: string) => Promise<boolean>
export type CommandModule = { run: CommandFunc }
