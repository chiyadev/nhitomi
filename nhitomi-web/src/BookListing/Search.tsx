import { Select, Typography } from 'antd'
import { SelectProps } from 'antd/lib/select'
import React, { useContext, useState } from 'react'
import { useAsync } from 'react-use'
import { BookQuery, BookQueryTags, BookTag, QueryMatchMode, SuggestItem } from '../Client'
import { ClientContext } from '../ClientContext'
import { LayoutContext } from '../LayoutContext'
import { TagColors, TagDisplay } from '../Tags'

export const Search = ({ query, setQuery, total }: {
  query: BookQuery
  setQuery: (query: BookQuery) => void
  total: number
}) => {
  const client = useContext(ClientContext)
  const { width: windowWidth } = useContext(LayoutContext)

  const [search, setSearch] = useState('')
  const [suggestions, setSuggestions] = useState<{ tag: BookTag, items: SuggestItem[] }[]>([])

  const { loading } = useAsync(async () => {
    if (!search) {
      setSuggestions([])
      return
    }

    const { tags } = await client.book.suggestBooks({
      suggestQuery: {
        limit: 50,
        fuzzy: true,
        prefix: search
      }
    })

    setSuggestions(Object
      .keys(tags)
      .map(key => {
        const tag = key as BookTag

        if (tags[tag]?.length)
          return { tag, items: tags[tag]!.sort((a, b) => b.score - a.score) }

        return { tag, items: [] }
      })
      .filter(x => x.items.length)
      .sort((a, b) => b.items[0].score - a.items[0].score))
  }, [search])

  query.tags = query.tags || {}

  const selectedValues = Object.keys(query.tags).flatMap(key => query.tags![key as BookTag]?.values?.map(v => `${key}:${v}`) || [])

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
          const { tag, value } = parseValue(b)

          if (!a[tag])
            a[tag] = { values: [], mode: QueryMatchMode.All }

          a[tag]!.values.push(value)
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
    }}>

    {suggestions.map(({ tag, items }) => {
      items = items.filter(v => !selectedValues.includes(`${tag}:${v}`))

      if (!items.length)
        return null

      return <Select.OptGroup key={tag} label={tag}>
        {items.map(({ text }) => {
          const value = `${tag}:${text}`

          return <Select.Option key={value} value={value}>
            <Typography.Text style={{ color: TagColors[tag] }}>{text}</Typography.Text>
          </Select.Option>
        })}
      </Select.OptGroup>
    })}
  </Select>
}

const Tag: SelectProps<string>['tagRender'] = ({ value: valueRaw, ...props }) => {
  const { tag, value } = parseValue(valueRaw as string)

  return <TagDisplay {...props} tag={tag} value={value} />
}

function parseValue(s: string) {
  const i = s.indexOf(':')

  return {
    tag: s.substring(0, i) as BookTag,
    value: s.substring(i + 1)
  }
}
