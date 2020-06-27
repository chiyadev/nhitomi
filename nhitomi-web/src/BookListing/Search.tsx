import { Select, Typography, Input, Menu, Dropdown, Button, Checkbox, Tooltip } from 'antd'
import { SelectProps } from 'antd/lib/select'
import React, { useContext, useState, useMemo, CSSProperties, useLayoutEffect, useRef } from 'react'
import { useAsync } from 'react-use'
import { BookTag, BookSuggestResultTags, LanguageType, ScraperCategory, BookSort, SortDirection } from '../Client'
import { ClientContext } from '../ClientContext'
import { TagColors, TagDisplay, TagLabels } from '../Tags'
import { BookListingContext } from '.'
import { useUpdateOnEvent } from '../hooks'
import { FormattedMessage, useIntl } from 'react-intl'
import { FlagIcon, FlagIconCode } from 'react-flag-kit'
import { languageNames } from '../LocaleProvider'
import { EllipsisOutlined, SortAscendingOutlined, SortDescendingOutlined } from '@ant-design/icons'
import { SourceIcon } from '../SourceButton'

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

    <MoreQueries />
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

  const suggestionsId = useRef(0)

  const { loading } = useAsync(async () => {
    if (!search) {
      setSuggestions({})
      return
    }

    const id = ++suggestionsId.current

    const { tags } = await client.book.suggestBooks({
      suggestQuery: {
        limit: 50,
        fuzzy: true,
        prefix: search
      }
    })

    if (id === suggestionsId.current)
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
            ...manager.query,
            type: 'simple',
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
            ...manager.query,
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
              ...manager.query,
              type: 'simple',
              value
            }
            break
        }
      }} />
  )
}

const MoreQueries = () => {
  const client = useContext(ClientContext)
  const { manager } = useContext(BookListingContext)

  useUpdateOnEvent(manager, 'query')

  const [visible, setVisible] = useState(false)
  const [langsExpanded, setLangsExpanded] = useState(false)

  const languages = useMemo(() => {
    const languages = Object.values(LanguageType)

    let items = languages.map(lang => {
      let label = <span>{languageNames[lang]}</span>

      if (lang === manager.query.language)
        label = <strong>{label}</strong>

      return (
        <Menu.Item
          key={lang}
          icon={<FlagIcon key={lang} code={lang.split('-')[1] as FlagIconCode} />}
          onClick={() => manager.query = { ...manager.query, language: lang }}>

          <span> {label}</span>
        </Menu.Item>
      )
    })

    const defaultLangs = 4

    if (languages.indexOf(manager.query.language) >= defaultLangs)
      setLangsExpanded(true)

    if (!langsExpanded)
      items = [
        ...items.slice(0, defaultLangs),

        <Menu.Item
          key='more'
          icon={<EllipsisOutlined />}
          onClick={() => setLangsExpanded(true)} />
      ]

    return <Menu.ItemGroup title={<FormattedMessage id='bookListing.search.more.languages' />} children={items} />
  }, [manager.query, langsExpanded])

  const sorting = useMemo(() => (
    <Menu.ItemGroup title={<>
      <FormattedMessage id='bookListing.search.more.sorting' />
      {' '}
      <Tooltip title={<FormattedMessage id={`sortDirections.${manager.query.sort.direction}`} />}>
        {manager.query.sort.direction === SortDirection.Ascending
          ? <SortAscendingOutlined onClick={() => manager.query = { ...manager.query, sort: { ...manager.query.sort, direction: SortDirection.Descending } }} />
          : <SortDescendingOutlined onClick={() => manager.query = { ...manager.query, sort: { ...manager.query.sort, direction: SortDirection.Ascending } }} />}
      </Tooltip>
    </>}>
      {Object.values(BookSort).map(sort => (
        <Menu.Item
          key={sort}
          onClick={() => manager.query = { ...manager.query, sort: { ...manager.query.sort, value: sort } }}>

          <FormattedMessage id={`bookSorts.${sort}`} />
        </Menu.Item>
      ))}
    </Menu.ItemGroup>
  ), [manager.query])

  const sources = useMemo(() => (
    <Menu.ItemGroup title={<FormattedMessage id='bookListing.search.more.sources' />}>
      {client.currentInfo.scrapers.filter(s => s.category === ScraperCategory.Book).map(({ type, name }) => (
        <Menu.Item
          key={type}
          onClick={() => manager.query = { ...manager.query, sources: toggleArray(manager.query.sources, type) }}>

          <Checkbox checked={manager.query.sources.indexOf(type) !== -1}>
            <SourceIcon
              type={type}
              style={{
                width: 'auto',
                height: '2em'
              }} />

            <span> {name}</span>
          </Checkbox>
        </Menu.Item>
      ))}
    </Menu.ItemGroup>
  ), [client.currentInfo.scrapers, manager.query])

  return (
    <Dropdown
      visible={visible}
      onVisibleChange={v => { setVisible(v); if (!v) { setLangsExpanded(v) } }}
      placement='bottomRight'
      overlay={(
        <Menu
          selectedKeys={[manager.query.language, manager.query.sort.value]}
          onClick={e => e.domEvent.preventDefault()}>

          {languages}
          {sorting}
          {sources}
        </Menu>
      )}>

      <Button icon={<EllipsisOutlined />} />
    </Dropdown>
  )
}

function toggleArray<T>(array: T[], value: T) {
  const index = array.indexOf(value)

  if (index === -1)
    return [...array, value]

  return array.filter((_, i) => i !== index)
}
