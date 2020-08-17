import { BookTag } from 'nhitomi-api'
import { ColorHue } from '../theme'

export const BookTagColors: { [key in BookTag]: ColorHue } = {
  artist: 'orange',
  circle: 'indigo',
  character: 'yellow',
  // copyright: 'red',
  parody: 'green',
  series: 'fuschia',
  // pool: 'cyan',
  convention: 'pink',
  metadata: 'lime',
  tag: 'blue'
}
