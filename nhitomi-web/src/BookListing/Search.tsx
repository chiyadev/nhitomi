import { Select, Typography } from 'antd'
import { SelectProps } from 'antd/lib/select'
import React, { useContext, useState, useMemo, useLayoutEffect } from 'react'
import { useAsync } from 'react-use'
import { BookTag, BookSuggestResultTags } from '../Client'
import { ClientContext } from '../ClientContext'
import { LayoutContext } from '../LayoutContext'
import { TagColors, TagDisplay, TagLabels } from '../Tags'
import { BookListingContext } from '.'
import { TagQueryItem } from './searchManager'

export const Search = () => {
  const client = useContext(ClientContext)
  const { width: windowWidth } = useContext(LayoutContext)
  const { manager } = useContext(BookListingContext)

  const [selected, setSelected] = useState<string[]>([])
  const [search, setSearch] = useState('')
  const [suggestions, setSuggestions] = useState<BookSuggestResultTags>({})

  useLayoutEffect(() => {
    const set = (items: TagQueryItem[]) => setSelected(items.map(({ type, value }) => `${type}:${value}`))

    set(manager.tagQuery)

    manager.on('tagQuery', set)
    return () => { manager.off('tagQuery', set) }
  }, [manager])

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
  }, [search, selected])

  return (
    <Select
      // no autoFocus because scroll position will be lost on navigation when it's enabled
      allowClear
      mode='multiple'
      loading={loading}
      notFoundContent={false}
      tagRender={Tag}
      value={selected}
      onChange={newValues => {
        setSearch('')

        manager.tagQuery = newValues.map(v => {
          const delimiter = v.indexOf(':')
          return { type: v.substring(0, delimiter) as BookTag, value: v.substring(delimiter + 1) }
        })
      }}
      searchValue={search}
      onSearch={setSearch}
      placeholder={`Search ${manager.total} books...`}
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
              .filter(({ value }) => !selected.includes(value))
              .map(({ item, value }) => ({
                label: <Typography.Text style={{ color: TagColors[tag] }}>{item.text}</Typography.Text>,
                value
              }))
          })),
        [suggestions, selected])} />
  )
}

const Tag: SelectProps<string>['tagRender'] = ({ value: value2, ...props }) => {
  const value = value2 as string
  const delimiter = value.indexOf(':')

  return (
    <TagDisplay
      {...props}
      tag={value.substring(0, delimiter) as BookTag}
      value={value.substring(delimiter + 1)}
      expandable={false} />
  )
}
