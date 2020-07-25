import { Tag } from 'antd'
import { ComponentProps, useState, ReactNode } from 'react'
import React from 'react'
import { BookCategory, BookTag, LanguageType, MaterialRating } from './Client'
import { presetPrimaryColors } from '@ant-design/colors'

import theme from './theme.json'
import { FormattedMessage } from 'react-intl'

// override preset with colors from theme.json
Object
  .keys(theme)
  .forEach(k => presetPrimaryColors[k] = theme[k as keyof typeof theme] || presetPrimaryColors[k])

/** Display names of tags. */
export const TagLabels: { [tag in BookTag]: ReactNode } = {
  artist: <FormattedMessage id='bookTags.artist' />,
  circle: <FormattedMessage id='bookTags.circle' />,
  character: <FormattedMessage id='bookTags.character' />,
  parody: <FormattedMessage id='bookTags.parody' />,
  series: <FormattedMessage id='bookTags.series' />,
  convention: <FormattedMessage id='bookTags.convention' />,
  metadata: <FormattedMessage id='bookTags.metadata' />,
  tag: <FormattedMessage id='bookTags.tag' />
}

/** List of all book tags in correct display order. */
export const BookTagList = Object.keys(TagLabels) as BookTag[]

/** Preset colors for tags. */
export const TagColorPresets: { [tag in BookTag]: string } = {
  artist: 'volcano',
  circle: 'orange',
  character: 'gold',
  // copyright: 'red',
  parody: 'green',
  series: 'magenta',
  // pool: 'cyan',
  convention: 'yellow',
  metadata: 'lime',
  tag: 'blue'
}

/** Hex colors for tags. */
export const TagColors = Object.keys(TagColorPresets).reduce((a, b) => { a[b as keyof typeof TagColorPresets] = presetPrimaryColors[TagColorPresets[b as keyof typeof TagColorPresets]]; return a }, {} as typeof TagColorPresets)

export const TagDisplay = ({ tag, value, ...props }: { tag: keyof typeof TagLabels, value: string } & TagProps) =>
  <ExpandableTag type={tag} value={value} color={TagColorPresets[tag]} {...props} />

/** Display names of categories. */
export const CategoryLabels: { [category in BookCategory]: ReactNode } = {
  doujinshi: <FormattedMessage id='bookCategories.doujinshi' />,
  manga: <FormattedMessage id='bookCategories.manga' />,
  artistCg: <FormattedMessage id='bookCategories.artistCg' />,
  gameCg: <FormattedMessage id='bookCategories.gameCg' />,
  lightNovel: <FormattedMessage id='bookCategories.lightNovel' />
}

export const CategoryDescriptions: typeof CategoryLabels = {
  doujinshi: <FormattedMessage id='bookCategories.descriptions.doujinshi' />,
  manga: <FormattedMessage id='bookCategories.descriptions.manga' />,
  artistCg: <FormattedMessage id='bookCategories.descriptions.artistCg' />,
  gameCg: <FormattedMessage id='bookCategories.descriptions.gameCg' />,
  lightNovel: <FormattedMessage id='bookCategories.descriptions.lightNovel' />
}

/** List of all book tags in correct display order. */
export const BookCategoryList = Object.keys(CategoryLabels) as BookCategory[]

/** Preset colors for categories. */
export const CategoryColorPresets: { [category in BookCategory]: string } = {
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
export const LanguageTypeLabels: { [language in LanguageType]: string } = {
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
export const LanguageTypeColorPresets: { [language in LanguageType]: string } = {
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
export const MaterialRatingLabels: { [rating in MaterialRating]: ReactNode } = {
  safe: <FormattedMessage id='materialRatings.safe' />,
  questionable: <FormattedMessage id='materialRatings.questionable' />,
  explicit: <FormattedMessage id='materialRatings.explicit' />
}

export const MaterialRatingDescriptions: typeof MaterialRatingLabels = {
  safe: <FormattedMessage id='materialRatings.descriptions.safe' />,
  questionable: <FormattedMessage id='materialRatings.descriptions.questionable' />,
  explicit: <FormattedMessage id='materialRatings.descriptions.explicit' />
}

/** List of all material ratings in correct display order. */
export const MaterialRatingList = Object.keys(MaterialRatingLabels) as MaterialRating[]

/** Preset colors for material ratings. */
export const MaterialRatingColorPresets: { [rating in MaterialRating]: string } = {
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
