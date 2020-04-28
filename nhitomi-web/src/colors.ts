import { presetPrimaryColors } from '@ant-design/colors'
import { BookCategory, BookTag, LanguageType, MaterialRating } from './Client'

import colorOverrides from './colors.json'

// override preset with colors from colors.json
Object
  .keys(colorOverrides)
  .forEach(k => presetPrimaryColors[k] = colorOverrides[k as keyof typeof colorOverrides] || presetPrimaryColors[k])

/** Preset colors for tags. */
export const TagColorPresets: { [key in BookTag]: string } = {
  // artist
  artist: 'volcano',

  // circle
  circle: 'orange',

  // character
  character: 'gold',

  // copyright
  // copyright: 'red',

  // parody
  parody: 'green',

  // series
  series: 'magenta',

  // pool
  // pool: 'cyan',

  // convention
  convention: 'yellow',

  // metadata
  metadata: 'purple',

  // general
  tag: 'blue'
}

/** Hex colors for tags. */
export const TagColors = Object.keys(TagColorPresets).reduce((a, b) => { a[b as keyof typeof TagColorPresets] = presetPrimaryColors[TagColorPresets[b as keyof typeof TagColorPresets]]; return a }, {} as typeof TagColorPresets)

/** Preset colors for categories. */
export const CategoryColorPresets: { [key in BookCategory]: string } = {
  doujinshi: 'volcano',
  manga: 'yellow',
  artistCg: 'cyan',
  gameCg: 'lime',
  lightNovel: 'purple'
}

/** Hex colors for categories. */
export const CategoryColors = Object.keys(CategoryColorPresets).reduce((a, b) => { a[b as keyof typeof CategoryColorPresets] = presetPrimaryColors[CategoryColorPresets[b as keyof typeof CategoryColorPresets]]; return a }, {} as typeof CategoryColorPresets)

/** Preset colors for language types. */
export const LanguageTypeColorPresets: { [key in LanguageType]: string } = {
  'ja-JP': 'cyan',
  'en-US': 'purple',
  'zh-CN': 'orange',
  'ko-KR': 'geekblue',
  'it-IT': 'grey',
  'es-ES': 'grey',
  'de-DE': 'grey',
  'fr-FR': 'grey',
  'tr-TR': 'grey',
  'nl-nl': 'grey',
  'ru-RU': 'grey',
  'id-ID': 'grey',
  'vi-VN': 'grey'
}

/** Hex colors for language types. */
export const LanguageTypeColors = Object.keys(LanguageTypeColorPresets).reduce((a, b) => { a[b as keyof typeof LanguageTypeColorPresets] = presetPrimaryColors[LanguageTypeColorPresets[b as keyof typeof LanguageTypeColorPresets]]; return a }, {} as typeof LanguageTypeColorPresets)

/** Preset colors for material ratings. */
export const MaterialRatingColorPresets: { [key in MaterialRating]: string } = {
  safe: 'green',
  questionable: 'volcano',
  explicit: 'magenta'
}

/** Hex colors for material ratings. */
export const MaterialRatingColors = Object.keys(MaterialRatingColorPresets).reduce((a, b) => { a[b as keyof typeof MaterialRatingColorPresets] = presetPrimaryColors[MaterialRatingColorPresets[b as keyof typeof MaterialRatingColorPresets]]; return a }, {} as typeof MaterialRatingColorPresets)
