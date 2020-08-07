import React, { useState, useRef, useLayoutEffect, RefObject, useMemo, ReactElement } from 'react'
import { useUrlState } from '../url'
import { SearchQuery } from './search'
import { SearchOutlined } from '@ant-design/icons'
import { usePageState } from '../Prefetch'
import { PrefetchResult } from '.'
import { cx, css } from 'emotion'
import { colors } from '../theme.json'
import { BookTag } from 'nhitomi-api'
import { BookTagColors } from '../Components/colors'
import Tippy from '@tippyjs/react'

type QueryToken = {
  index: number
  type: 'other'
  text: string
  display: string
} | {
  index: number
  type: 'tag'
  text: string
  tag: BookTag
  value: string
  display: string
}

const tagRegex = /(?<tag>\w+):(?<value>\S+)/gsi
const allTags = Object.values(BookTag)

function tokenize(text: string): QueryToken[] {
  const results: ReturnType<typeof tokenize> = []
  let match: RegExpExecArray | null
  let start = 0

  while ((match = tagRegex.exec(text))) {
    const tag = (match.groups?.tag || '') as BookTag
    const value = match.groups?.value || ''

    if (allTags.findIndex(t => t.toLowerCase() === tag.toLowerCase()) === -1)
      continue

    if (start < match.index) {
      const s = text.substring(start, match.index)

      results.push({
        index: start,
        type: 'other',
        text: s,
        display: s.trim().replace('_', ' ')
      })
    }

    results.push({
      index: match.index,
      type: 'tag',
      text: text.substring(match.index, tagRegex.lastIndex),
      tag,
      value,
      display: value.trim().replace('_', ' ')
    })

    start = tagRegex.lastIndex
  }

  if (start < text.length) {
    const s = text.substring(start, text.length)

    results.push({
      index: start,
      type: 'other',
      text: s,
      display: s.trim().replace('_', ' ')
    })
  }

  return results
}

export const SearchInput = () => {
  const [result] = usePageState<PrefetchResult>('fetch')
  const [query, setQuery] = useUrlState<SearchQuery>('replace')

  const [text, setText] = useState('')
  const tokens = useMemo(() => tokenize(text), [text])
  const inputRef = useRef<HTMLInputElement>(null)

  return (
    <div className='mx-auto p-4 w-full max-w-xl'>
      <div className='shadow-lg w-full flex flex-row bg-white text-black border-none rounded overflow-hidden'>
        <Autocompleter
          tokens={tokens}
          inputRef={inputRef}>

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
        </Autocompleter>

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
      })}
    </div>
  )
}

const Autocompleter = ({ tokens, inputRef, children }: { tokens: QueryToken[], inputRef: RefObject<HTMLInputElement>, children?: ReactElement<any> }) => {
  const [index, setIndex] = useState<number>()
  const [focused, setFocused] = useState(false)

  useLayoutEffect(() => {
    const input = inputRef.current

    if (!input)
      return

    const caretHandler = () => {
      const index = input.selectionEnd || input.selectionStart
      setIndex(typeof index === 'number' ? index : undefined)
    }

    const focusHandler = () => {
      setFocused(document.activeElement === input)
    }

    // unfortunately input doesn't have a caret event
    input.addEventListener('mousedown', caretHandler)
    input.addEventListener('mouseup', caretHandler)
    input.addEventListener('keydown', caretHandler)
    input.addEventListener('keyup', caretHandler)

    input.addEventListener('focus', focusHandler)
    input.addEventListener('blur', focusHandler)

    return () => {
      input.removeEventListener('mousedown', caretHandler)
      input.removeEventListener('mouseup', caretHandler)
      input.removeEventListener('keydown', caretHandler)
      input.removeEventListener('keyup', caretHandler)

      input.removeEventListener('focus', focusHandler)
      input.removeEventListener('blur', focusHandler)
    }
  }, [inputRef])

  const token = useMemo(() => {
    if (typeof index !== 'number')
      return

    let match = tokens[tokens.length - 1]

    for (const token of tokens.slice().reverse()) {
      if (token.display && token.index <= index && index <= token.index + token.text.length)
        match = token
    }

    if (match?.display)
      return match
  }, [index, tokens])

  return (
    <Tippy
      visible={focused && !!token}
      interactive
      arrow={false}
      placement='bottom-start'
      maxWidth={inputRef.current?.clientWidth}
      content={(
        <div className={css`width: ${inputRef.current?.clientWidth}px;`}>
          {token && <>
            <span className='text-xs'>{token.display}</span>
          </>}
        </div>
      )}
      children={children} />
  )
}
