import { Select, Typography, Input } from 'antd'
import { SelectProps } from 'antd/lib/select'
import React, { useContext, useState, useMemo, CSSProperties, useLayoutEffect } from 'react'
import { useAsync } from 'react-use'
import { BookTag, BookSuggestResultTags } from '../Client'
import { ClientContext } from '../ClientContext'
import { TagColors, TagDisplay, TagLabels } from '../Tags'
import { BookListingContext } from '.'
import { LanguageSelect } from '../LanguageSelect'
import { useUpdateOnEvent } from '../hooks'
import { FormattedMessage, useIntl } from 'react-intl'

export const Search = () => {
  const { manager } = useContext(BookListingContext)

  useUpdateOnEvent(manager, 'query')

  const style: CSSProperties = {
    flex: 1,
    minWidth: '20em'
  }

  return <Input.Group compact style={{
    display: 'flex',
    flexDirection: 'row',
    width: '100%'
  }}>
    {manager.query.type === 'tag' ? <TagSearch style={style} />
      : manager.query.type === 'simple' ? <SimpleSearch style={style} />
        : null}

    <LangSelect />
  </Input.Group>
}

const TagSearch = ({ style }: {
  style?: CSSProperties
}) => {
  const client = useContext(ClientContext)
  const { manager } = useContext(BookListingContext)

  useUpdateOnEvent(manager, 'query')
  useUpdateOnEvent(manager, 'result')

  const selected = useMemo(() => {
    if (manager.query.type === 'tag')
      return manager.query.items.map(({ type, value }) => `${type}:${value}`)

    return []
  }, [manager.query])

  const [search, setSearch] = useState('')
  const [suggestions, setSuggestions] = useState<BookSuggestResultTags>({})

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
      style={style}
      // no autoFocus because scroll position will be lost on navigation when it's enabled
      allowClear
      mode='multiple'
      loading={loading}
      notFoundContent={false}
      tagRender={Tag}
      value={selected}
      onChange={newValues => {
        setSearch('')

        // if there are any values with no type prefix or if there is a link, switch to simple query
        if (newValues.some(v => {
          const delimiter = v.indexOf(':')
          return delimiter === -1 || v.substring(delimiter + 1).startsWith('//')
        })) {
          manager.query = {
            type: 'simple',
            language: manager.query.language,
            value: newValues.map(v => {
              let value = v.substring(v.indexOf(':') + 1)

              if (value.startsWith('//'))
                value = v

              // booru style spaces
              value = value.replace('_', ' ')

              // wrap around quotes for phrase match
              if (value.indexOf(' ') !== -1)
                value = `"${value}"`

              return value
            }).join(' ')
          }
        }
        else {
          manager.query = {
            type: 'tag',
            language: manager.query.language,
            items: newValues.map(v => {
              const delimiter = v.indexOf(':')
              return { type: v.substring(0, delimiter) as BookTag, value: v.substring(delimiter + 1) }
            })
          }
        }
      }}
      searchValue={search}
      onSearch={setSearch}
      placeholder={<FormattedMessage id='bookListing.search.total' values={{ total: manager.result.total }} />}
      options={[
        // suggestions
        ...useMemo(() => (
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
            }))
        ), [suggestions, selected]),

        // custom input suggestion
        ...(!search || (Object.keys(suggestions) as BookTag[]).some(t => suggestions[t]?.some(x => x.text === search)) ? [] : [{
          label: <FormattedMessage id='bookListing.search.otherTag' />,
          options: [{
            label: ((s: string) => {
              const delimiter = s.indexOf(':')
              let type = delimiter === -1 ? undefined : s.substring(0, delimiter)
              let value = s.substring(delimiter + 1)

              if (value.startsWith('//')) {
                type = 'metadata'
                value = s
              }

              return <Typography.Text style={{ color: TagColors[type as BookTag] }}>{value || s}</Typography.Text>
            })(search),
            value: search
          }]
        }])
      ]} />
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

export const SimpleSearch = ({ style }: {
  style?: CSSProperties
}) => {
  const { manager } = useContext(BookListingContext)
  const { formatMessage } = useIntl()

  useUpdateOnEvent(manager, 'query')
  useUpdateOnEvent(manager, 'result')

  const [value, setValue] = useState(manager.query.type === 'simple' ? manager.query.value : '')

  useLayoutEffect(() => setValue(manager.query.type === 'simple' ? manager.query.value : ''), [manager.query])

  return (
    <Input
      style={style}
      value={value}
      onChange={({ target: { value } }) => setValue(value)}
      placeholder={formatMessage({ id: 'bookListing.search.total' }, { total: manager.result.total })}
      onKeyDown={e => {
        switch (e.keyCode) {
          case 13:
            manager.query = {
              type: 'simple',
              language: manager.query.language,
              value
            }
            break
        }
      }} />
  )
}

const LangSelect = () => {
  const { manager } = useContext(BookListingContext)

  useUpdateOnEvent(manager, 'query')

  return (
    <LanguageSelect
      value={manager.query.language}
      setValue={v => manager.query = { ...manager.query, language: v }}
      style={{
        display: 'inline-block',
        height: '100%'
      }} />
  )
}
