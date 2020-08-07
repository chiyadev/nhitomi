import React, { useState, useRef, useLayoutEffect, RefObject, useMemo, ReactElement, useCallback, Dispatch } from 'react'
import { useUrlState } from '../url'
import { SearchQuery } from './search'
import { SearchOutlined } from '@ant-design/icons'
import { usePageState } from '../Prefetch'
import { PrefetchResult } from '.'
import { cx, css } from 'emotion'
import { colors } from '../theme.json'
import { BookTag, SuggestItem } from 'nhitomi-api'
import { BookTagColors } from '../Components/colors'
import Tippy from '@tippyjs/react'
import { useClient } from '../ClientManager'
import { useNotify } from '../NotificationManager'

export type QueryToken = {
  type: 'other'
  index: number
  begin: number
  end: number
  text: string
  display: string
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
const allTags = Object.values(BookTag)

export function tokenize(text: string): QueryToken[] {
  const results: ReturnType<typeof tokenize> = []
  let match: RegExpExecArray | null
  let start = 0

  const addOther = (start: number, end: number) => {
    const s = text.substring(start, end)

    results.push({
      type: 'other',
      index: start,
      begin: start + (s.length - s.trimStart().length),
      end: start + s.trimEnd().length,
      text: s,
      display: s.replace(/_/g, ' ').trim()
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

    if (allTags.findIndex(t => t.toLowerCase() === tag.toLowerCase()) === -1)
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

export const SearchInput = () => {
  const [result] = usePageState<PrefetchResult>('fetch')
  const [query, setQuery] = useUrlState<SearchQuery>('replace')

  const [text, setText] = useState('')
  const tokens = useMemo(() => tokenize(text), [text])
  const inputRef = useRef<HTMLInputElement>(null)

  useLayoutEffect(() => setText(query.query || ''), [query.query])

  return (
    <div className='mx-auto p-4 w-full max-w-xl'>
      <div className='shadow-lg w-full flex flex-row bg-white text-black border-none rounded overflow-hidden'>
        <Suggestor
          tokens={tokens}
          inputRef={inputRef}
          setText={setText}>

          <div className='flex-grow text-sm relative overflow-hidden'>
            <input
              ref={inputRef}
              className={cx('pl-4 w-full h-full absolute top-0 left-0 border-none', css`
                background: none;
                color: transparent;
                caret-color: black;
                z-index: 1;

                &::placeholder {
                  color: ${colors.gray[800]};
                }
                &::selection {
                  color: white;
                  background: ${colors.blue[600]};
                }
              `)}
              value={text}
              onChange={({ target: { value } }) => setText(value)}
              placeholder={`Search ${result?.total} books`} />

            <Highlighter
              tokens={tokens}
              inputRef={inputRef}
              className='pl-4 w-full h-full absolute top-0 left-0' />
          </div>
        </Suggestor>

        <div className='text-white px-3 py-2 bg-blue-600 text-lg'>
          <SearchOutlined className='align-middle' />
        </div>
      </div>
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
    <div className={cx('leading-8 flex items-center whitespace-pre', css`left: ${offset}px;`, className)}>
      {tokens.map(token => {
        switch (token.type) {
          case 'other':
            return (
              <span key={token.index}>{token.text}</span>
            )

          case 'tag':
            return (
              <span key={token.index}>
                <span className={css`opacity: 30%;`}>{token.tag}:</span>
                <span className={css`color: ${BookTagColors[token.tag]};`}>{token.value}</span>
              </span>
            )
        }

        return null
      })}
    </div>
  )
}

const Suggestor = ({ tokens, setText, inputRef, children }: { tokens: QueryToken[], setText: Dispatch<string>, inputRef: RefObject<HTMLInputElement>, children?: ReactElement<any> }) => {
  const [index, setIndex] = useState<number>()
  const [focused, setFocused] = useState(false)
  const [suggestions, setSuggestions] = useState<{ tag: BookTag, items: SuggestItem[] }[]>()
  const [selected, setSelected] = useState<SuggestItem>()

  const token = useMemo(() => {
    if (typeof index === 'number')
      return tokens.slice().reverse().find(token => token.display && token.begin <= index)
  }, [index, tokens])

  const complete = useCallback(() => {
    if (!selected || !token) return

    const tag = suggestions?.find(s => s.items.indexOf(selected) !== -1)?.tag
    let text = assemble(tokens)

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
  }, [inputRef, selected, setText, suggestions, token, tokens])

  useLayoutEffect(() => {
    const input = inputRef.current
    if (!input) return

    const handler = () => {
      const index = input.selectionEnd || input.selectionStart
      setIndex(typeof index === 'number' ? index : undefined)
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
      setFocused(document.activeElement === input)
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
        setSelected(items[(items.length + (selected ? items.indexOf(selected) : 0) + move) % items.length])
      }

      switch (e.keyCode) {
        case 38: moveSelected(-1); break  // up
        case 40: moveSelected(1); break   // down
        case 13: complete(); break        // enter

        default: return
      }

      e.preventDefault()
    }

    input.addEventListener('keydown', handler)
    return () => input.removeEventListener('keydown', handler)
  }, [complete, inputRef, selected, suggestions])

  const client = useClient()
  const { notifyError } = useNotify()
  const suggestionId = useRef(0)
  const suggestPrefix = token?.display

  useLayoutEffect(() => {
    if (!suggestPrefix) {
      suggestionId.current++
      setSuggestions(undefined)
      setSelected(undefined)
      return
    }

    let id = ++suggestionId.current

    setTimeout(async () => {
      if (id !== suggestionId.current)
        return

      id = ++suggestionId.current

      try {
        const result = await client.book.suggestBooks({
          suggestQuery: {
            prefix: suggestPrefix,
            limit: 50
          }
        })

        if (id !== suggestionId.current)
          return

        const suggestions = Object
          .keys(result.tags)
          .sort((a, b) => (result.tags[b as BookTag]?.[0]?.score || 0) - (result.tags[a as BookTag]?.[0]?.score || 0))
          .map(key => ({
            tag: key as BookTag,
            items: result.tags[key as BookTag] || []
          }))
          .filter(x => x.items.length)

        setSuggestions(suggestions)
        setSelected(suggestions[0]?.items[0])
      }
      catch (e) {
        notifyError(e)
      }
    }, 100)
  }, [client.book, notifyError, suggestPrefix])

  return (
    <Tippy
      visible={focused && !!token}
      interactive
      arrow={false}
      placement='bottom-start'
      maxWidth={inputRef.current?.clientWidth}
      content={(
        <div className={css`width: ${inputRef.current?.clientWidth}px; max-width: 100%;`}>
          {token && <>
            <span className='text-xs opacity-50'>"{token.display}"</span>
          </>}

          {suggestions && suggestions.map(({ tag, items }) => (
            <ul key={tag} className='my-2'>
              <li>
                <span className={cx('text-xs', css`color: ${BookTagColors[tag]};`)}>{tag}</span>
              </li>

              {items.map(item => (
                <li>
                  <span
                    key={item.id}
                    className={cx('block bg-opacity-25 rounded-sm cursor-pointer', { 'bg-gray-100': selected === item })}
                    onMouseDown={complete}
                    onMouseEnter={() => setSelected(item)}>

                    {item.text}
                  </span>
                </li>
              ))}
            </ul>
          ))}
        </div>
      )}
      children={children} />
  )
}
