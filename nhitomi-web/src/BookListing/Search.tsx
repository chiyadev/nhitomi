import { Select, Typography } from 'antd'
import { SelectProps } from 'antd/lib/select'
import React, { useContext, useState, useMemo } from 'react'
import { useAsync } from 'react-use'
import { BookTag, BookSuggestResultTags, BookQuery } from '../Client'
import { ClientContext } from '../ClientContext'
import { LayoutContext } from '../LayoutContext'
import { TagColors, TagDisplay, TagLabels } from '../Tags'
import { setQueryTags, flattenQueryTags } from './queryHelper'

export const Search = ({ query, setQuery, total }: {
  query: BookQuery
  setQuery: (v: BookQuery) => void
  total: number
}) => {
  const client = useContext(ClientContext)
  const { width: windowWidth } = useContext(LayoutContext)

  const [search, setSearch] = useState('')
  const [suggestions, setSuggestions] = useState<BookSuggestResultTags>({})

  const selectedValues = useMemo(() => flattenQueryTags(query).map(({ tag, value }) => `${tag}:${value}`), [query])

  const { loading } = useAsync(async () => {
    if (!search) {
      setSuggestions({})
      return
    }

    const { tags } = await client.book.suggestBooks({
      suggestQuery: {
        limit: 50,
        fuzzy: true,
        prefix: search
      }
    })

    setSuggestions(tags)
  }, [search, selectedValues])

  return <Select
    // no autoFocus because scroll position will be lost on navigation when it's enabled
    allowClear
    mode='multiple'
    loading={loading}
    notFoundContent={false}
    tagRender={Tag}
    value={selectedValues}
    onChange={newValues => {
      setSearch('')
      setQuery(setQueryTags(query, newValues.map(parseValue)))
    }}
    searchValue={search}
    onSearch={setSearch}
    placeholder={`Search ${total} books...`}
    style={{
      width: '100%',
      minWidth: '20em',
      maxWidth: windowWidth / 2
    }}
    options={useMemo(() =>
      (Object.keys(suggestions) as BookTag[])
        .filter(tag => suggestions[tag]?.length)
        .sort((a, b) => suggestions[b]![0].score - suggestions[a]![0].score)
        .map(tag => ({
          label: TagLabels[tag],
          options: suggestions[tag]!
            .map(item => ({ item, value: `${tag}:${item.text}` }))
            .filter(({ value }) => !selectedValues.includes(value))
            .map(({ item, value }) => ({
              label: <Typography.Text style={{ color: TagColors[tag] }}>{item.text}</Typography.Text>,
              value
            }))
        })),
      [
        suggestions,
        selectedValues
      ])} />
}

const Tag: SelectProps<string>['tagRender'] = ({ value, ...props }) => {
  const { tag, value: text } = parseValue(value as string)

  return <TagDisplay {...props} tag={tag} value={text} expandable={false} />
}

function parseValue(s: string) {
  const i = s.indexOf(':')

  return {
    tag: s.substring(0, i) as BookTag,
    value: s.substring(i + 1)
  }
}
