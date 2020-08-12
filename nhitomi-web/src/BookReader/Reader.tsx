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
    const pages = content.pageCount

    layoutEngine.initialize(pages)
    setImages(new Array(pages))
  }, [book, content, layoutEngine])

  const layout = useMemo(() => {
    return layoutEngine.recompute(images || [], {
      viewportWidth,
      viewportHeight
    })
  }, [images, layoutEngine, viewportHeight, viewportWidth])
  console.log('layout update', layout)

  const setImage = useMemo(() => {
    const list: Dispatch<ImageBase | undefined>[] = []

    for (let i = 0; i < content.pageCount; i++) {
      const index = i

      list.push(image => {
        setImages(images => {
          if (!images)
            return

          const array = images.slice()
          array[index] = image
          return array
        })
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

const Page = ({ book, content, index, image: { x, y, width, height }, setImage }: {
  book: Book
  content: BookContent
  index: number
  image: LayoutImage
  setImage: Dispatch<ImageBase | undefined>
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
            onLoad={async () => await client.book.getBookImage({ id: book.id, contentId: content.id, index })}
            onLoaded={setImage} />
        )}
      </div>
    </VisibilitySensor>
  )
}
