import { Select, Typography } from 'antd'
import { SelectProps } from 'antd/lib/select'
import React, { useContext, useState, useMemo } from 'react'
import { useAsync } from 'react-use'
import { BookQueryTags, BookTag, QueryMatchMode, BookSuggestResultTags } from '../Client'
import { ClientContext } from '../ClientContext'
import { LayoutContext } from '../LayoutContext'
import { TagColors, TagDisplay, TagLabels } from '../Tags'
import { BookListingContext } from '.'

export const Search = () => {
  const client = useContext(ClientContext)
  const { width: windowWidth } = useContext(LayoutContext)
  const { query, setQuery, total } = useContext(BookListingContext)

  const [search, setSearch] = useState('')
  const [suggestions, setSuggestions] = useState<BookSuggestResultTags>({})

  const selectedValues = useMemo(() => Object.keys(query.tags || {}).flatMap(key => query.tags?.[key as BookTag]?.values?.map(v => `${key}:${v}`) || []), [query.tags])

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
  }, [search])

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
      setQuery({
        ...query,
        tags: newValues.reduce((a, b) => {
          const { tag, text } = parseValue(b)

          if (!a[tag])
            a[tag] = { values: [], mode: QueryMatchMode.All }

          a[tag]!.values.push(text)
          return a
        }, {} as BookQueryTags)
      })
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
  const { tag, text } = parseValue(value as string)

  return <TagDisplay {...props} tag={tag} value={text} />
}

function parseValue(s: string) {
  const i = s.indexOf(':')

  return {
    tag: s.substring(0, i) as BookTag,
    text: s.substring(i + 1)
  }
}
