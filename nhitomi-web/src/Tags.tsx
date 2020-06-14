import { Tag } from 'antd'
import { ComponentProps, useState } from 'react'
import React from 'react'
import { BookCategory, BookTag, LanguageType, MaterialRating } from './Client'
import { presetPrimaryColors } from '@ant-design/colors'

import colorOverrides from './colors.json'

// override preset with colors from colors.json
Object
  .keys(colorOverrides)
  .forEach(k => presetPrimaryColors[k] = colorOverrides[k as keyof typeof colorOverrides] || presetPrimaryColors[k])

/** Display names of tags. */
export const TagLabels: { [key in BookTag]: string } = {
  artist: 'Artist',
  circle: 'Circle',
  character: 'Character',
  parody: 'Parody',
  series: 'Series',
  convention: 'Convention',
  metadata: 'Metadata',
  tag: 'Tag'
}

/** List of all book tags in correct display order. */
export const BookTagList = Object.values(BookTag).sort((a, b) => Object.keys(TagLabels).indexOf(a) - Object.keys(TagLabels).indexOf(b))

/** Preset colors for tags. */
export const TagColorPresets: typeof TagLabels = {
  artist: 'volcano',
  circle: 'orange',
  character: 'gold',
  // copyright: 'red',
  parody: 'green',
  series: 'magenta',
  // pool: 'cyan',
  convention: 'yellow',
  metadata: 'purple',
  tag: 'blue'
}

/** Hex colors for tags. */
export const TagColors = Object.keys(TagColorPresets).reduce((a, b) => { a[b as keyof typeof TagColorPresets] = presetPrimaryColors[TagColorPresets[b as keyof typeof TagColorPresets]]; return a }, {} as typeof TagColorPresets)

export const TagDisplay = ({ tag, value, ...props }: { tag: keyof typeof TagLabels, value: string } & TagProps) =>
  <ExpandableTag type={tag} value={value} color={TagColorPresets[tag]} {...props} />

/** Display names of categories. */
export const CategoryLabels: { [key in BookCategory]: string } = {
  doujinshi: 'Doujinshi',
  manga: 'Manga',
  artistCg: 'Artist CG',
  gameCg: 'Game CG',
  lightNovel: 'Light novel'
}

export const CategoryDescriptions: typeof CategoryLabels = {
  doujinshi: 'Self-published or self-distributed hentai manga or pornographic comic involving recognizable characters or mascots from other anime, manga or video games.',
  manga: 'Officially published pornographic comic, typically featuring original content.',
  artistCg: 'Artwork sets without panels, often made digitally, typically in full color.',
  gameCg: 'Images extracted from erotic games ("eroge").',
  lightNovel: 'High-quality scanned images of light novels.'
}

/** List of all book tags in correct display order. */
export const BookCategoryList = Object.values(BookCategory).sort((a, b) => Object.keys(CategoryLabels).indexOf(a) - Object.keys(CategoryLabels).indexOf(b))

/** Preset colors for categories. */
export const CategoryColorPresets: typeof CategoryLabels = {
  doujinshi: 'volcano',
  manga: 'yellow',
  artistCg: 'cyan',
  gameCg: 'lime',
  lightNovel: 'purple'
}

/** Hex colors for categories. */
export const CategoryColors = Object.keys(CategoryColorPresets).reduce((a, b) => { a[b as keyof typeof CategoryColorPresets] = presetPrimaryColors[CategoryColorPresets[b as keyof typeof CategoryColorPresets]]; return a }, {} as typeof CategoryColorPresets)

export const CategoryDisplay = ({ category, ...props }: { category: keyof typeof CategoryLabels } & TagProps) =>
  <ExpandableTag type='category' value={category} color={CategoryColorPresets[category]} {...props} />

/** Display names of language types. */
export const LanguageTypeLabels: { [key in LanguageType]: string } = {
  'ja-JP': 'Japanese',
  'en-US': 'English', // English (US)
  'zh-CN': 'Chinese', // Chinese (PRC)
  'ko-KR': 'Korean',
  'it-IT': 'Italian',
  'es-ES': 'Spanish',
  'de-DE': 'German',
  'fr-FR': 'French',
  'tr-TR': 'Turkish',
  'nl-NL': 'Dutch',
  'ru-RU': 'Russian',
  'id-ID': 'Indonesian',
  'vi-VN': 'Vietnamese'
}

/** List of all language types in correct display order. */
export const LanguageTypeList = Object.keys(LanguageTypeLabels) as LanguageType[]

/** Preset colors for language types. */
export const LanguageTypeColorPresets: typeof LanguageTypeLabels = {
  'ja-JP': 'cyan',
  'en-US': 'yellow',
  'zh-CN': 'orange',
  'ko-KR': 'geekblue',
  'it-IT': 'purple',
  'es-ES': 'purple',
  'de-DE': 'purple',
  'fr-FR': 'purple',
  'tr-TR': 'purple',
  'nl-NL': 'purple',
  'ru-RU': 'purple',
  'id-ID': 'purple',
  'vi-VN': 'purple'
}

/** Hex colors for language types. */
export const LanguageTypeColors = Object.keys(LanguageTypeColorPresets).reduce((a, b) => { a[b as keyof typeof LanguageTypeColorPresets] = presetPrimaryColors[LanguageTypeColorPresets[b as keyof typeof LanguageTypeColorPresets]]; return a }, {} as typeof LanguageTypeColorPresets)

export const LanguageTypeDisplay = ({ language, ...props }: { language: LanguageType } & TagProps) =>
  <ExpandableTag type='language' value={LanguageTypeLabels[language]} color={LanguageTypeColorPresets[language]} {...props} />

/** Display names of material ratings. */
export const MaterialRatingLabels: { [key in MaterialRating]: string } = {
  safe: 'Safe',
  questionable: 'Questionable',
  explicit: 'Explicit'
}

export const MaterialRatingDescriptions: typeof MaterialRatingLabels = {
  safe: 'Clearly lacking any sexual content.',
  questionable: 'Containing non-genital nudity, sexually suggestive acts, or something that is not generically considered pornographic.',
  explicit: 'Containing exposed genitals, openly and unambiguously portrayed sex acts, or visible sexual fluids.'
}

/** List of all material ratings in correct display order. */
export const MaterialRatingList = Object.keys(MaterialRatingLabels) as MaterialRating[]

/** Preset colors for material ratings. */
export const MaterialRatingColorPresets: typeof MaterialRatingLabels = {
  safe: 'green',
  questionable: 'volcano',
  explicit: 'magenta'
}

/** Hex colors for material ratings. */
export const MaterialRatingColors = Object.keys(MaterialRatingColorPresets).reduce((a, b) => { a[b as keyof typeof MaterialRatingColorPresets] = presetPrimaryColors[MaterialRatingColorPresets[b as keyof typeof MaterialRatingColorPresets]]; return a }, {} as typeof MaterialRatingColorPresets)

export const MaterialRatingDisplay = ({ rating, ...props }: { rating: MaterialRating } & TagProps) =>
  <ExpandableTag type='rating' value={rating} color={MaterialRatingColorPresets[rating]} {...props} />

type TagProps = Omit<ComponentProps<typeof ExpandableTag>, 'type' | 'value'>

/** Base component for tags that can expand on hover. */
export const ExpandableTag = ({ type, value, expandable = true, children, style, onClick, ...props }: { type: string, value: string, expandable?: boolean } & ComponentProps<typeof Tag>) => {
  const [expand, setExpand] = useState(false)

  return <Tag
    onMouseEnter={() => setExpand(true)}
    onMouseLeave={() => setExpand(false)}
    style={{
      cursor: onClick ? 'pointer' : undefined, // link-like when clickable
      ...style
    }}
    onClick={onClick}
    {...props}>

    {expand && expandable
      ? <>{type}: <strong>{value}</strong></>
      : <>{value}</>}

    {children}
  </Tag>
}
