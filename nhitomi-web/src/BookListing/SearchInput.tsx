import React, { useState, useRef, useLayoutEffect, RefObject, useMemo, ReactElement, useCallback, Dispatch } from 'react'
import { useQueryState } from '../state'
import { SearchQuery } from './search'
import { SearchOutlined, CloseOutlined } from '@ant-design/icons'
import { cx, css } from 'emotion'
import { BookTag, SuggestItem, BookSearchResult } from 'nhitomi-api'
import { BookTagColors } from '../Components/colors'
import Tippy from '@tippyjs/react'
import { useClient } from '../ClientManager'
import { useNotify } from '../NotificationManager'
import { useSpring, animated, useTransition } from 'react-spring'
import useResizeObserver from '@react-hook/resize-observer'
import { useLocalized } from '../LocaleManager'
import { FormattedMessage } from 'react-intl'
import { BookTags } from '../orderedConstants'
import { convertHex, getColor } from '../theme'
import { useShortcut } from '../shortcut'

export type QueryToken = {
  type: 'other'
  index: number
  begin: number
  end: number
  text: string
  display: string
  url: boolean
} | {
  type: 'tag'
  index: number
  begin: number // same as index
  end: number
  text: string
  tag: BookTag
  value: string
  display: string
}

const tagRegex = /(?<tag>\w+):(?<value>\S+)/gsi

export function tokenize(text: string): QueryToken[] {
  const results: ReturnType<typeof tokenize> = []
  let match: RegExpExecArray | null
  let start = 0

  const addOther = (start: number, end: number) => {
    const s = text.substring(start, end)
    let url = false

    try {
      url = !!new URL(s)
    }
    catch { /* ignored */ }

    results.push({
      type: 'other',
      index: start,
      begin: start + (s.length - s.trimStart().length),
      end: start + s.trimEnd().length,
      text: s,
      display: s.replace(/_/g, ' ').trim(),
      url
    })
  }

  const addTag = (start: number, end: number, tag: BookTag, value: string) => {
    results.push({
      type: 'tag',
      index: start,
      begin: start,
      end,
      text: text.substring(start, end),
      tag,
      value,
      display: value.replace(/_/g, ' ').trim()
    })
  }

  while ((match = tagRegex.exec(text))) {
    const tag = (match.groups?.tag || '') as BookTag
    const value = match.groups?.value || ''

    if (BookTags.findIndex(t => t.toLowerCase() === tag.toLowerCase()) === -1)
      continue

    if (start < match.index) {
      addOther(start, match.index)
    }

    addTag(match.index, tagRegex.lastIndex, tag, value)
    start = tagRegex.lastIndex
  }

  if (start < text.length) {
    addOther(start, text.length)
  }

  return results
}

export function assemble(tokens: QueryToken[]): string {
  return tokens.map(token => token.text).join('')
}

export const SearchInput = ({ result, className }: { result: BookSearchResult, className?: string }) => {
  const [query, setQuery] = useQueryState<SearchQuery>('push')
  const [text, setText] = useState('')
  const [focused, setFocused] = useState(false)

  const tokens = useMemo(() => tokenize(text), [text])
  const inputRef = useRef<HTMLInputElement>(null)

  useLayoutEffect(() => setText(query.query || ''), [query.query])

  useShortcut('bookListingSearchKey', () => inputRef.current?.focus())

  const setTextWithSearch = useCallback((text: string) => { setText(text); setQuery({ ...query, query: text }) }, [query, setQuery])
  const placeholder = useLocalized('pages.bookListing.search', { total: result?.total })

  const input = useMemo(() => (
    <Suggestor
      tokens={tokens}
      inputRef={inputRef}
      setText={setTextWithSearch}>

      <div className='flex-grow relative overflow-hidden'>
        <input
          ref={inputRef}
          className={cx('pl-4 w-full h-full absolute top-0 left-0 border-none', css`
            background: none;
            color: transparent;
            caret-color: black;
            z-index: 1;

            &::placeholder {
              color: ${getColor('gray', 'darker').hex};
            }
            &::selection {
              color: white;
              background: ${getColor('blue').opacity(0.5).hex};
            }
          `)}
          value={text}
          onChange={({ target: { value } }) => setText(value)}
          onFocus={() => setFocused(true)}
          onBlur={() => setFocused(false)}
          placeholder={placeholder} />

        <Highlighter
          tokens={tokens}
          inputRef={inputRef}
          className='pl-4 w-full h-full absolute top-0 left-0' />
      </div>
    </Suggestor>
  ), [placeholder, setTextWithSearch, text, tokens])

  return useMemo(() => (
    <div className={cx('flex flex-row bg-white text-black rounded overflow-hidden', className)}>
      {input}

      <ClearButton onClick={() => setTextWithSearch('')} visible={!!text && !focused} className='right-0' />
      <SearchButton onClick={() => setTextWithSearch(text)} />
    </div>
  ), [className, focused, input, setTextWithSearch, text])
}

const SearchButton = ({ onClick }: { onClick?: () => void }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    backgroundColor: convertHex('#fff', hover ? 0.2 : 0),
    transform: hover ? 'scale(1.1)' : 'scale(1)'
  })

  return (
    <div
      className='text-white bg-blue cursor-pointer select-none'
      onMouseDown={onClick}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      <animated.div className='px-4 py-3 text-lg' style={style}>
        <SearchOutlined />
      </animated.div>
    </div>
  )
}

const ClearButton = ({ visible, onClick, className }: { visible?: boolean, onClick?: () => void, className?: string }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    opacity: visible ? 1 : 0
  })

  const iconStyle = useSpring({
    transform: hover ? 'scale(1.1)' : 'scale(1)'
  })

  return (
    <div className='relative'>
      <animated.div
        style={style} // bg-white to display above text
        className={cx('absolute right-0 bg-white h-full flex items-center z-10 px-4 cursor-pointer select-none', { 'pointer-events-none': !visible }, className)}
        onMouseDown={onClick}
        onMouseEnter={() => setHover(true)}
        onMouseLeave={() => setHover(false)}>

        <animated.span style={iconStyle} className='text-sm text-gray-darker'>
          <CloseOutlined />
        </animated.span>
      </animated.div>
    </div>
  )
}

const Highlighter = ({ tokens, inputRef, className }: { tokens: QueryToken[], inputRef: RefObject<HTMLInputElement>, className?: string }) => {
  const [offset, setOffset] = useState(0)

  useLayoutEffect(() => {
    const input = inputRef.current

    if (!input)
      return

    const handler = () => setOffset(-input.scrollLeft)

    input.addEventListener('scroll', handler)
    return () => input.removeEventListener('scroll', handler)
  }, [inputRef])

  return (
    <div style={{ marginLeft: offset }} className={cx('leading-8 flex items-center whitespace-pre', className)}>
      {useMemo(() => tokens.map(token => {
        switch (token.type) {
          case 'other':
            return (
              <span className={cx({ 'text-cyan': token.url })} key={token.index}>{token.text}</span>
            )

          case 'tag':
            return (
              <span key={token.index}>
                <span className='text-gray'>{token.tag}:</span>
                <span className={`text-${BookTagColors[token.tag]}`}>{token.value}</span>
              </span>
            )

          default:
            return null
        }
      }), [tokens])}
    </div>
  )
}

const Suggestor = ({ tokens, setText, inputRef, children }: { tokens: QueryToken[], setText: Dispatch<string>, inputRef: RefObject<HTMLInputElement>, children?: ReactElement<any> }) => {
  const [index, setIndex] = useState<number>()
  const [visible, setVisible] = useState(false)
  const [suggestions, setSuggestions] = useState<{ tag: BookTag, items: SuggestItem[] }[]>()
  const [selected, setSelected] = useState<SuggestItem>()

  const token = useMemo(() => {
    if (typeof index === 'number')
      return tokens.slice().reverse().find(token => token.display && token.begin <= index)
  }, [index, tokens])

  const complete = useCallback(() => {
    // hides the overlay so that it doesn't cover the listing after searching
    const hide = () => setTimeout(() => {
      setVisible(false)
      setSuggestions(undefined)
    })

    let text = assemble(tokens)

    // can trigger search in 'other' tokens
    if (!selected || !token) {
      setText(text)
      hide()
      return
    }

    const tag = suggestions?.find(s => s.items.indexOf(selected) !== -1)?.tag

    const remove = (s: string, start: number, end: number) => s.substring(0, start) + s.substring(end)
    const insert = (s: string, index: number, value: string) => s.substring(0, index) + value + s.substring(index)

    const replacement = `${tag}:${selected.text.replace(/\s/g, '_')}`

    text = remove(text, token.begin, token.end)
    text = insert(text, token.begin, replacement)

    const caret = token.begin + replacement.length + 1

    if (text[caret] !== ' ')
      text = insert(text, caret, ' ')

    setText(text)

    setTimeout(() => {
      const input = inputRef.current

      if (input) {
        input.selectionStart = input.selectionEnd = caret
        input.focus()
      }
    })

    hide()
  }, [inputRef, selected, setText, suggestions, token, tokens])

  useLayoutEffect(() => {
    const input = inputRef.current
    if (!input) return

    const handler = () => {
      const index = input.selectionEnd || input.selectionStart

      setIndex(typeof index === 'number' ? index : undefined)
      setVisible(true)
    }

    // unfortunately input doesn't have a caret event
    input.addEventListener('mousedown', handler)
    input.addEventListener('mouseup', handler)
    input.addEventListener('keydown', handler)
    input.addEventListener('keyup', handler)

    return () => {
      input.removeEventListener('mousedown', handler)
      input.removeEventListener('mouseup', handler)
      input.removeEventListener('keydown', handler)
      input.removeEventListener('keyup', handler)
    }
  }, [inputRef])

  useLayoutEffect(() => {
    const input = inputRef.current
    if (!input) return

    const handler = () => {
      setVisible(document.activeElement === input)
    }

    input.addEventListener('focus', handler)
    input.addEventListener('blur', handler)

    return () => {
      input.removeEventListener('focus', handler)
      input.removeEventListener('blur', handler)
    }
  }, [inputRef])

  useLayoutEffect(() => {
    const input = inputRef.current
    if (!input) return

    const handler = (e: KeyboardEvent) => {
      const moveSelected = (move: number) => {
        const items = suggestions?.flatMap(({ items }) => items) || []
        const newItem = items[(items.length + (selected ? items.indexOf(selected) : 0) + move) % items.length]

        setSelected(newItem)
      }

      const moveTokenSelected = (move: number) => {
        if (!tokens.length)
          return

        const tokenns = tokens.filter(token => token.display)
        const newToken = tokenns[(tokenns.length + (token ? tokenns.indexOf(token) : 0) + move) % tokenns.length]

        input.selectionStart = newToken.begin
        input.selectionEnd = newToken.end
      }

      switch (e.keyCode) {
        case 38: moveSelected(-1); break  // up
        case 40: moveSelected(1); break   // down
        case 13: complete(); break        // enter
        case 27: input.blur(); break      // escape

        // tab
        case 9: moveTokenSelected(e.shiftKey ? -1 : 1); break

        default: return
      }

      e.preventDefault()
    }

    input.addEventListener('keydown', handler)
    return () => input.removeEventListener('keydown', handler)
  }, [complete, inputRef, selected, suggestions, token, tokens])

  const client = useClient()
  const { notifyError } = useNotify()
  const suggestId = useRef(0)
  const [suggestLoading, setSuggestLoading] = useState(true)
  const suggestPrefix = token?.display

  useLayoutEffect(() => {
    if (!suggestPrefix) {
      suggestId.current++
      setSuggestions(undefined)
      setSelected(undefined)
      return
    }

    let id = ++suggestId.current

    setSuggestLoading(true)

    setTimeout(async () => {
      try {
        if (id !== suggestId.current)
          return

        id = ++suggestId.current

        const result = await client.book.suggestBooks({
          suggestQuery: {
            prefix: suggestPrefix,
            limit: 50
          }
        })

        if (id !== suggestId.current)
          return

        const suggestions = [...BookTags]
          .sort((a, b) => (result.tags[b]?.[0]?.score || 0) - (result.tags[a]?.[0]?.score || 0))
          .map(tag => ({
            tag,
            items: result.tags[tag] || []
          }))
          .filter(x => x.items.length)

        setSuggestions(suggestions)
        setSelected(suggestions.flatMap(s => s.items).find(s => s.id === selected?.id) || suggestions[0]?.items[0])
        setSuggestLoading(false)
      }
      catch (e) {
        notifyError(e)
      }
    }, 200)
  }, [suggestPrefix]) // eslint-disable-line

  const dropdownVisible = visible && !!token
  const dropdownStyle = useSpring({
    opacity: dropdownVisible ? 1 : 0,
    marginTop: dropdownVisible ? 0 : -5
  })

  const [dropdownWidth, setDropdownWidth] = useState(inputRef.current?.clientWidth)

  useResizeObserver(inputRef, ({ contentRect: { width } }) => setDropdownWidth(width))

  const suggestionsTransitions = useTransition(suggestions || [], {
    from: { opacity: 0 },
    enter: { opacity: 1 },
    leave: { display: 'none' }
  })

  const dropdownContent = <>
    {useMemo(() => token && (
      <span className='text-sm text-gray-darker'>"{token.display}" ({suggestions && !suggestLoading ? suggestions.flatMap(s => s.items).length : '*'})</span>
    ), [suggestLoading, suggestions, token])}

    {suggestionsTransitions((style, { tag, items }) => (
      <animated.ul key={tag} style={style}>
        <li className={cx(`text-sm text-${BookTagColors[tag]}`)}>
          <FormattedMessage id={`types.bookTag.${tag}`} />
        </li>

        <SuggestorSection items={items} complete={complete} selected={selected} setSelected={setSelected} />
      </animated.ul>
    ))}
  </>

  return (
    <Tippy
      visible={dropdownVisible}
      interactive
      placement='bottom-start'
      maxWidth={dropdownWidth}
      render={props => (
        <animated.div
          {...props}
          style={{ ...dropdownStyle, width: inputRef.current?.clientWidth }}
          className='bg-gray-darkest bg-blur text-white px-2 py-1 rounded overflow-hidden flex flex-col space-y-2'>

          {dropdownContent}
        </animated.div>
      )}
      children={children} />
  )
}

const SuggestorSection = ({ items, complete, selected, setSelected }: { items: SuggestItem[], complete: () => void, selected?: SuggestItem, setSelected: Dispatch<SuggestItem> }) => {
  const transitions = useTransition(items, {
    from: { marginLeft: -5, opacity: 0, display: 'none' },
    enter: { marginLeft: 0, opacity: 1, display: 'block' },
    leave: { display: 'none' }
  })

  return <>{transitions((style, item) => (
    <animated.li
      key={item.id}
      style={style}
      onMouseDown={complete}
      onMouseEnter={() => setSelected(item)}>

      <span className={cx('block bg-opacity-50 rounded-sm overflow-hidden cursor-pointer px-10 -mx-10', { 'bg-black': selected === item })}>
        {item.text}
      </span>
    </animated.li>
  ))}</>
}
