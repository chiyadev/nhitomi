import { BookTag } from 'nhitomi-api'
import { colors } from '../theme.json'

export const BookTagColors: { [key in BookTag]: string } = {
  artist: colors.orange[700],
  circle: colors.indigo[700],
  character: colors.yellow[700],
  // copyright: 'red',
  parody: colors.green[700],
  series: colors.fuschia[700],
  // pool: 'cyan',
  convention: colors.pink[700],
  metadata: colors.lime[700],
  tag: colors.blue[700]
}
