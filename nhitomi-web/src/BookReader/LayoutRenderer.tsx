import { FetchImage, FetchManagerContext } from './fetchManager'
import { useMemo, useContext, CSSProperties, useState } from 'react'
import { LayoutManager, LayoutImage } from './layoutManager'
import { Book, BookContent } from '../Client'
import React from 'react'
import { LayoutContext } from '../LayoutContext'
import { useScrollShortcut } from '../shortcuts'
import ReactVisibilitySensor from 'react-visibility-sensor'
import { Alert, Typography, Button } from 'antd'
import { FormattedMessage } from 'react-intl'
import { ReloadOutlined } from '@ant-design/icons'

export const LayoutRenderer = ({ book, content, fetched }: {
  book: Book
  content: BookContent
  fetched: (FetchImage | undefined)[]
}) => {
  useScrollShortcut()

  const { width: viewportWidth, height: viewportHeight } = useContext(LayoutContext)

  const manager = useMemo(() => new LayoutManager(book, content), [book, content])
  const { width, height, layout } = useMemo(() => manager.recalculate(fetched, {
    viewportWidth,
    viewportHeight,
    viewportBound: false
  }), [
    manager,
    fetched,
    viewportWidth,
    viewportHeight
  ])

  return <div style={{
    position: 'relative',
    width,
    height
  }}>
    {useMemo(() => layout.map((image, i) => <Image key={i} layoutImage={image} />), [layout])}
  </div>
}

const Image = ({ layoutImage }: { layoutImage: LayoutImage }) => useMemo(() => {
  if (!layoutImage.image)
    return null

  const { image, x, y, width, height } = layoutImage

  const style: CSSProperties = {
    position: 'absolute',
    left: x,
    top: y,
    width,
    height
  }

  if (image.done === true)
    return <img src={image.data} alt={image.data} style={style} />

  if (image.done instanceof Error)
    return <ErrorImage image={image} style={style} error={image.done} />

  return null
}, [
  layoutImage
])

const ErrorImage = ({ image, style, error }: { image: FetchImage, style: CSSProperties, error: Error }) => {
  const [visible, setVisible] = useState(false)
  const fetch = useContext(FetchManagerContext)

  return <ReactVisibilitySensor onChange={setVisible} partialVisibility>
    <div style={style}>
      {visible && <div style={{
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
      </div>}
    </div>
  </ReactVisibilitySensor>
}
