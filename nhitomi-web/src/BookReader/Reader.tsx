import React, { useMemo, useState, useLayoutEffect, Dispatch } from 'react'
import { Book, BookContent } from 'nhitomi-api'
import { LayoutEngine, ImageBase, LayoutImage } from './layoutEngine'
import { useLayout } from '../LayoutManager'
import { CoverImage } from '../Components/CoverImage'
import { useClient } from '../ClientManager'
import VisibilitySensor from 'react-visibility-sensor'

export const Reader = ({ book, content, viewportWidth }: { book: Book, content: BookContent, viewportWidth: number }) => {
  const { height: viewportHeight } = useLayout()

  const layoutEngine = useMemo(() => new LayoutEngine(), [])
  const [images, setImages] = useState<(ImageBase | undefined)[]>()

  useLayoutEffect(() => {
    layoutEngine.initialize(content.pageCount)

    setImages(new Array(content.pageCount))
  }, [book, content, layoutEngine])

  const layout = useMemo(() => {
    return layoutEngine.recompute(images || [], {
      viewportWidth,
      viewportHeight
    })
  }, [images, layoutEngine, viewportHeight, viewportWidth])

  const setImage = useMemo(() => {
    const list: Dispatch<ImageBase | undefined>[] = []

    for (let i = 0; i < content.pageCount; i++) {
      list.push(newImage => {
        setImages(images => images?.map((image, index) => {
          if (index === i)
            return newImage
          else
            return image
        }))
      })
    }

    return list
  }, [content])

  return (
    <div className='relative' style={{
      width: layout.width,
      height: layout.height
    }}>
      {useMemo(() => (
        layout.images.map((image, i) => (
          <Page
            book={book}
            content={content}
            index={i}
            image={image}
            setImage={setImage[i]} />
        ))
      ), [book, content, layout.images, setImage])}
    </div>
  )
}

const Page = ({ book, content, index, image: { x, y, width, height } }: {
  book: Book
  content: BookContent
  index: number
  image: LayoutImage
  setImage: Dispatch<LayoutImage | undefined>
}) => {
  const client = useClient()
  const [showImage, setShowImage] = useState(false)

  return (
    <VisibilitySensor
      onChange={v => { v && setShowImage(true) }}
      partialVisibility
      offset={{ top: -500, bottom: -500 }}>

      <div className='absolute' style={{
        top: y,
        left: x,
        width,
        height
      }}>
        {showImage && (
          <CoverImage
            className='w-full h-full'
            sizing='contain'
            onLoad={async () => await client.book.getBookImage({ id: book.id, contentId: content.id, index })} />
        )}
      </div>
    </VisibilitySensor>
  )
}
