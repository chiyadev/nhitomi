import React, { useRef, useState } from 'react'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { Book, BookContent } from 'nhitomi-api'
import { useClient } from '../ClientManager'
import { useScrollShortcut } from '../shortcut'
import { PageContainer } from '../Components/PageContainer'
import { Container } from '../Components/Container'
import useResizeObserver from '@react-hook/resize-observer'
import { Info } from './Info'
import { Background } from './Background'
import { Reader } from './Reader'
import { LayoutSetter } from './LayoutSetter'
import { CursorVisibility } from './CursorVisibility'
import { useTabTitle } from '../TitleSetter'
import { useConfig } from '../ConfigManager'
import { SupportBanner } from './SupportBanner'

export type PrefetchResult = { book: Book, content: BookContent }
export type PrefetchOptions = { id: string, contentId: string }

export const useBookReaderPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ id, contentId }) => {
  const client = useClient()

  return {
    destination: {
      path: `/books/${id}/contents/${contentId}`
    },

    fetch: async () => {
      const book = await client.book.getBook({ id })
      const content = book.contents.find(c => c.id === contentId)

      if (!content)
        throw Error(`Content ${contentId} does not exist in book ${id}.`)

      return { book, content }
    }
  }
}

export const BookReaderLink = ({ id, contentId, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useBookReaderPrefetch} options={{ id, contentId }} {...props} />
)

export const BookReader = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useBookReaderPrefetch, { requireAuth: true, ...options })

  useScrollShortcut()

  if (!result)
    return null

  return (
    <PageContainer key={`${result.book.id}/${result.content.id}`}>
      <Loaded book={result.book} content={result.content} />
    </PageContainer>
  )
}

const Loaded = ({ book, content }: PrefetchResult) => {
  const [preferEnglishName] = useConfig('bookReaderPreferEnglishName')

  useTabTitle((preferEnglishName && book.englishName) || book.primaryName)

  const infoRef = useRef(null)
  const [{ width: infoWidth, height: infoHeight }, setInfoSize] = useState<{ width: number, height: number }>({ width: 0, height: 0 })

  useResizeObserver(infoRef, ({ contentRect }) => setInfoSize(contentRect))

  return <>
    <LayoutSetter />
    <Background book={book} content={content} scrollHeight={infoHeight} />

    <div className='space-y-8'>
      <div ref={infoRef}>
        <Container>
          <Info book={book} content={content} />
        </Container>
      </div>

      <SupportBanner book={book} content={content} />

      <CursorVisibility>
        <Reader book={book} content={content} viewportWidth={infoWidth} />
      </CursorVisibility>
    </div>
  </>
}
