import React, { useState, useRef, useLayoutEffect, RefObject, useMemo } from 'react'
import { useUrlState } from '../url'
import { SearchQuery } from './search'
import { SearchOutlined } from '@ant-design/icons'
import { usePageState } from '../Prefetch'
import { PrefetchResult } from '.'
import { cx, css } from 'emotion'
import { colors } from '../theme.json'
import { BookTag } from 'nhitomi-api'
import { BookTagColors } from '../Components/colors'

export const SearchInput = () => {
  const [result] = usePageState<PrefetchResult>('fetch')
  const [query, setQuery] = useUrlState<SearchQuery>('replace')

  const [text, setText] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  return (
    <div className='shadow-lg mx-auto my-4 w-full max-w-xl flex flex-row bg-white text-black border-none rounded overflow-hidden'>
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
          value={text}
          inputRef={inputRef}
          className='pl-4 w-full h-full absolute top-0 left-0' />
      </div>

      <div className='text-white px-3 py-2 bg-blue-600 text-lg'>
        <SearchOutlined className='align-middle' />
      </div>
    </div>
  )
}

const tagRegex = /(?<tag>\w+):(?<value>\S+)/gsi
const allTags = Object.values(BookTag)

function tokenize(text: string): ({
  index: number
  type: 'other'
  text: string
} | {
  index: number
  type: 'tag'
  text: string
  tag: BookTag
  value: string
})[] {
  const results: ReturnType<typeof tokenize> = []
  let match: RegExpExecArray | null
  let start = 0

  while ((match = tagRegex.exec(text))) {
    const tag = (match.groups?.tag || '') as BookTag
    const value = match.groups?.value || ''

    if (allTags.findIndex(t => t.toLowerCase() === tag.toLowerCase()) === -1)
      continue

    if (start < match.index) {
      results.push({
        index: start,
        type: 'other',
        text: text.substring(start, match.index)
      })
    }

    results.push({
      index: match.index,
      type: 'tag',
      text: text.substring(match.index, tagRegex.lastIndex),
      tag,
      value
    })

    start = tagRegex.lastIndex
  }

  if (start < text.length) {
    results.push({
      index: start,
      type: 'other',
      text: text.substring(start, text.length)
    })
  }

  return results
}

const Highlighter = ({ value, inputRef, className }: { value: string, inputRef: RefObject<HTMLInputElement>, className?: string }) => {
  const [offset, setOffset] = useState(0)

  useLayoutEffect(() => {
    const input = inputRef.current

    if (!input)
      return

    const handler = () => setOffset(-input.scrollLeft)

    input.addEventListener('scroll', handler)
    return () => input.removeEventListener('scroll', handler)
  }, [inputRef])

  const tokens = useMemo(() => tokenize(value), [value])

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
