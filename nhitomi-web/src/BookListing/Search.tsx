import { Select, Typography, Input } from 'antd'
import { SelectProps } from 'antd/lib/select'
import React, { useContext, useState, useMemo, useLayoutEffect } from 'react'
import { useAsync } from 'react-use'
import { BookTag, BookSuggestResultTags, LanguageType } from '../Client'
import { ClientContext } from '../ClientContext'
import { TagColors, TagDisplay, TagLabels } from '../Tags'
import { BookListingContext } from '.'
import { TagQueryItem } from './searchManager'
import { LanguageSelect } from '../LanguageSelect'

export const Search = () => {
  const client = useContext(ClientContext)
  const { manager } = useContext(BookListingContext)

  const [selected, setSelected] = useState<string[]>([])
  const [total, setTotal] = useState(0)
  const [search, setSearch] = useState('')
  const [suggestions, setSuggestions] = useState<BookSuggestResultTags>({})

  useLayoutEffect(() => {
    const set = (items: TagQueryItem[]) => setSelected(items.map(({ type, value }) => `${type}:${value}`))

    set(manager.tagQuery)

    manager.on('tagQuery', set)
    return () => { manager.off('tagQuery', set) }
  }, [manager])

  useLayoutEffect(() => {
    setTotal(manager.total)

    manager.on('total', setTotal)
    return () => { manager.off('total', setTotal) }
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

  const select = (
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
      placeholder={`Search ${total} books...`}
      style={{
        flex: 1,
        minWidth: '20em'
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

  const [language, setLanguage] = useState(manager.language)

  useLayoutEffect(() => {
    manager.on('language', setLanguage)
    return () => { manager.off('language', setLanguage) }
  }, [manager])

  const languageSelect = (
    <LanguageSelect
      value={language}
      setValue={v => manager.language = v}
      style={{
        display: 'inline-block',
        height: '100%'
      }} />
  )

  return <Input.Group compact style={{
    display: 'flex',
    flexDirection: 'row',
    width: '100%'
  }}>
    {select}
    {languageSelect}
  </Input.Group>
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
