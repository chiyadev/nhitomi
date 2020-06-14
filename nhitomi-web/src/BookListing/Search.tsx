import { Select, Typography, Input } from 'antd'
import { SelectProps } from 'antd/lib/select'
import React, { useContext, useState, useMemo, CSSProperties } from 'react'
import { useAsync } from 'react-use'
import { BookTag, BookSuggestResultTags } from '../Client'
import { ClientContext } from '../ClientContext'
import { TagColors, TagDisplay, TagLabels } from '../Tags'
import { BookListingContext } from '.'
import { LanguageSelect } from '../LanguageSelect'
import { useUpdateOnEvent } from '../hooks'

export const Search = () => {
  const { manager } = useContext(BookListingContext)

  useUpdateOnEvent(manager, 'state')

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

  useUpdateOnEvent(manager, 'state')

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

        // if there are any values with no type prefix, switch to simple query
        if (newValues.some(v => v.indexOf(':') === -1)) {
          manager.query = {
            type: 'simple',
            value: newValues.map(v => {
              const delimiter = v.indexOf(':')

              if (delimiter === -1)
                return v

              // remove type prefix
              v = v.substring(delimiter + 1)

              // booru style spaces
              v = v.replace('_', ' ')

              // wrap around quotes for phrase match
              if (v.indexOf(' ') !== -1)
                v = `"${v}"`

              return v
            }).join(' ')
          }
        }
        else {
          manager.query = {
            type: 'tag',
            items: newValues.map(v => {
              const delimiter = v.indexOf(':')
              return { type: v.substring(0, delimiter) as BookTag, value: v.substring(delimiter + 1) }
            })
          }
        }
      }}
      searchValue={search}
      onSearch={setSearch}
      placeholder={`Search ${manager.total} books...`}
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
          label: 'Other',
          options: [{
            label: ((s: string) => {
              const delimiter = s.indexOf(':')
              const type = delimiter === -1 ? undefined : s.substring(0, delimiter)
              const value = s.substring(delimiter + 1)

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

  const [value, setValue] = useState(manager.query.type === 'simple' ? manager.query.value : '')

  return (
    <Input
      style={style}
      value={value}
      onChange={({ target: { value } }) => setValue(value)}
      placeholder={`Search ${manager.total} books...`}
      onKeyDown={e => {
        switch (e.keyCode) {
          case 13:
            manager.query = { type: 'simple', value }
            break
        }
      }} />
  )
}

const LangSelect = () => {
  const { manager } = useContext(BookListingContext)

  useUpdateOnEvent(manager, 'state')

  return (
    <LanguageSelect
      value={manager.language}
      setValue={v => manager.language = v}
      style={{
        display: 'inline-block',
        height: '100%'
      }} />
  )
}
