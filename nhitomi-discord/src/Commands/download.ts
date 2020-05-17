import { CommandFunc } from '.'
import { run as runInternal } from './get'

// todo: remove this deprecation notice completely in August 2020

export const run: CommandFunc = async (context, arg) => {
  await context.reply('\\*\\*Notice: nhitomi\'s Discord download command has been deprecated. You can use the web interface instead!')

  return await runInternal(context, arg)
}
