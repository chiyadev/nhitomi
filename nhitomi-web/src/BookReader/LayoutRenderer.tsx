import React, { useMemo, useContext, CSSProperties, useState, useRef, ReactElement } from 'react'
import { FetchImage } from './fetchManager'
import { LayoutManager, LayoutImage, LayoutManagerContext } from './layoutManager'
import { LayoutContext } from '../LayoutContext'
import { useScrollShortcut, useShortcutPress } from '../shortcuts'
import ReactVisibilitySensor from 'react-visibility-sensor'
import { Alert, Typography, Button, Spin } from 'antd'
import { FormattedMessage } from 'react-intl'
import { ReloadOutlined } from '@ant-design/icons'
import { useConfig } from '../Client/config'
import { ScrollPreserver } from './ScrollPreserver'
import { BookReaderContext } from '.'
import { ScrollManager } from './ScrollManager'

export const LayoutRenderer = ({ fetched }: { fetched: (FetchImage | undefined)[] }) => {
  useScrollShortcut()

  const { book, content } = useContext(BookReaderContext)
  const layout = useMemo(() => new LayoutManager(book, content), [book, content])

  const { width: viewportWidth, height: viewportHeight } = useContext(LayoutContext)
  const [imagesPerRow] = useConfig('bookReaderImagesPerRow')
  const [viewportBound] = useConfig('bookReaderViewportBound')
  const [leftToRight] = useConfig('bookReaderLeftToRight')
  const [singleCover] = useConfig('bookReaderSingleCover')

  const result = useMemo(() => layout.recalculate(fetched, {
    viewportWidth,
    viewportHeight,
    viewportBound,
    leftToRight,
    itemsPerRow: imagesPerRow,
    initialRowLimit: singleCover ? 1 : imagesPerRow
  }), [
    layout,
    fetched,
    viewportWidth,
    viewportHeight,
    viewportBound,
    leftToRight,
    imagesPerRow,
    singleCover
  ])

  const { width, height, images } = result

  const ref = useRef<HTMLDivElement>(null)

  return <div ref={ref} style={{
    position: 'relative',
    width,
    height
  }}>
    <ScrollPreserver containerRef={ref} layout={result} />
    <ScrollManager containerRef={ref} layout={result} />

    <LayoutManagerContext.Provider value={layout}>
      {useMemo(() => images.map((image, i) => <Image key={i} index={i} layoutImage={image} />), [images])}
    </LayoutManagerContext.Provider>
  </div>
}

const Image = ({ index, layoutImage }: {
  index: number
  layoutImage: LayoutImage
}) => {
  const { jump } = useContext(BookReaderContext)
  const [showNumber] = useShortcutPress('bookReaderPageNumberKey')

  return useMemo(() => {
    const { image, x, y, width, height } = layoutImage

    const style: CSSProperties = {
      position: 'absolute',
      left: x,
      top: y,
      width,
      height
    }

    let content: ReactElement

    if (image?.done === true)
      content = <img src={image.data} alt={image.data} style={style} />

    else if (image?.done instanceof Error)
      content = <ErrorDisplay image={image} style={style} error={image.done} />

    else
      content = <Spinner style={style} />

    if (jump || showNumber) content = <>
      {content}

      <div style={{
        ...style,
        backgroundColor: 'black',
        opacity: 0.5
      }}>
        <div style={{
          position: 'absolute',
          top: '50%',
          left: '50%',
          transform: 'translate(-50%, -50%)'
        }}>
          <strong style={{ fontSize: '6em' }}>{index + 1}</strong>
        </div>
      </div>
    </>

    return content
  }, [index, jump, layoutImage, showNumber])
}

const ErrorDisplay = ({ image, style, error }: { image: FetchImage, style: CSSProperties, error: Error }) => {
  const [visible, setVisible] = useState(false)
  const { fetch } = useContext(BookReaderContext)

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
          transform: 'translate(-50%, -50%)'
        }} />,
        [
          visible
        ])}
    </div>
  </ReactVisibilitySensor>
}
