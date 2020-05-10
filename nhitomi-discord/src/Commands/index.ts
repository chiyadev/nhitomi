import { MessageContext } from '../context'

export type CommandFunc = (context: MessageContext, arg?: string) => Promise<boolean>
export type CommandModule = { run: CommandFunc }
