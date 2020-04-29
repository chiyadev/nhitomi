import { FetchImage, FetchManagerContext } from './fetchManager'
import { useMemo, useContext, CSSProperties, useState, useRef } from 'react'
import { LayoutManager, LayoutImage, LayoutManagerContext } from './layoutManager'
import { Book, BookContent } from '../Client'
import React from 'react'
import { LayoutContext } from '../LayoutContext'
import { useScrollShortcut } from '../shortcuts'
import ReactVisibilitySensor from 'react-visibility-sensor'
import { Alert, Typography, Button, Spin } from 'antd'
import { FormattedMessage } from 'react-intl'
import { ReloadOutlined } from '@ant-design/icons'
import { useConfig } from '../Client/config'
import { ScrollPreserver } from './ScrollPreserver'

export const LayoutRenderer = ({ book, content, fetched }: {
  book: Book
  content: BookContent
  fetched: (FetchImage | undefined)[]
}) => {
  useScrollShortcut()

  const manager = useMemo(() => new LayoutManager(book, content), [book, content])

  const { width: viewportWidth, height: viewportHeight } = useContext(LayoutContext)
  const [imagesPerRow] = useConfig('bookReaderImagesPerRow')
  const [viewportBound] = useConfig('bookReaderViewportBound')
  const [leftToRight] = useConfig('bookReaderLeftToRight')
  const [singleCover] = useConfig('bookReaderSingleCover')
  // const [snapping] = useConfig('bookReaderSnapping')

  const { width, height, layout } = useMemo(() => manager.recalculate(fetched, {
    viewportWidth,
    viewportHeight,
    viewportBound,
    leftToRight,
    itemsPerRow: imagesPerRow,
    initialRowLimit: singleCover ? 1 : imagesPerRow
  }), [
    manager,
    fetched,
    viewportWidth,
    viewportHeight,
    viewportBound,
    leftToRight,
    imagesPerRow,
    singleCover
  ])

  const ref = useRef<HTMLDivElement>(null)

  return <div ref={ref} style={{
    position: 'relative',
    width,
    height
  }}>
    <ScrollPreserver containerRef={ref} />

    <LayoutManagerContext.Provider value={manager}>
      {useMemo(() => layout.map((image, i) => <Image key={i} layoutImage={image} />), [layout])}
    </LayoutManagerContext.Provider>
  </div>
}

const Image = ({ layoutImage }: { layoutImage: LayoutImage }) => useMemo(() => {
  const { image, x, y, width, height } = layoutImage

  const style: CSSProperties = {
    position: 'absolute',
    left: x,
    top: y,
    width,
    height
  }

  if (image?.done === true)
    return <img src={image.data} alt={image.data} style={style} />

  if (image?.done instanceof Error)
    return <ErrorDisplay image={image} style={style} error={image.done} />

  return <Spinner style={style} />
}, [
  layoutImage
])

const ErrorDisplay = ({ image, style, error }: { image: FetchImage, style: CSSProperties, error: Error }) => {
  const [visible, setVisible] = useState(false)
  const fetch = useContext(FetchManagerContext)

  return <ReactVisibilitySensor onChange={setVisible} partialVisibility>
    <div style={style}>
      {useMemo(() => visible &&
        <div style={{
          position: 'relative',
          top: '50%',
          left: '50%',
          transform: 'translate(-50%, -50%)',
          width: '100%',
          padding: '1em'
        }}>
          <Alert
            message={<FormattedMessage id='bookReader.pageError.title' />}
            description={<>
              <p>
                <FormattedMessage id='bookReader.pageError.description' />
                <br />
                <Typography.Text code copyable={{ text: error.stack }}>{error.message}</Typography.Text>
              </p>

              <Button style={{ float: 'right' }} type='ghost' icon={<ReloadOutlined />} onClick={() => fetch.retry(image)}>Retry</Button>
            </>}
            type='error'
            showIcon />
        </div>,
        [
          visible,
          error,
          fetch,
          image
        ])}
    </div>
  </ReactVisibilitySensor>
}

const Spinner = ({ style }: { style: CSSProperties }) => {
  const [visible, setVisible] = useState(false)

  return <ReactVisibilitySensor onChange={setVisible} partialVisibility>
    <div style={style}>
      {useMemo(() => visible &&
        <Spin style={{
          position: 'relative',
          left: '50%',
          top: '50%',
          transform: 'translate(-50%, -50%'
        }} />,
        [
          visible
        ])}
    </div>
  </ReactVisibilitySensor>
}
