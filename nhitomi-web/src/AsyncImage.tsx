import composeRefs from '@seznam/compose-react-refs'
import React, { CSSProperties, forwardRef, ReactNode, Ref, useContext, useRef, useState, useLayoutEffect } from 'react'
import { useAsync, useInterval, useUpdate } from 'react-use'
import VisibilitySensor from 'react-visibility-sensor'
import { LayoutContext } from './LayoutContext'
import { Spin } from 'antd'

/** Image component that loads images asynchronously with lazy loading, loading animation, aspect ratio preservation and advanced styling. */
export const AsyncImage = forwardRef(({ onVisibleChange, wrapperRef, src, onLoad, resize, width, height, fluid, naturalSize, centered, rounded, style, wrapperStyle, disabled, loadingDisabled, preloadScale = 2, children }: {
  /** Fired when visibility changes. */
  onVisibleChange?: (visible: boolean) => void

  /** Ref of the wrapper element. */
  wrapperRef?: Ref<HTMLDivElement>

  /** Image data source. */
  src: (() => Promise<Blob>) | Blob

  /** Called when src finished loading. */
  onLoad?: (blob: Blob) => void | Promise<void>

  /** Image resize mode, if frame width and height are specified. "fit" by default. */
  resize?: 'fit' | 'fill'

  /** Width of the image frame. */
  width?: number

  /** Height of the image frame. */
  height?: number

  /**
   * True to scale image proportionally to fit the containing element automatically.
   * This works by setting wrapper width to '100%'.
   * Width and height must be specified when this is true.
   */
  fluid?: boolean

  /**
   * True to override width and height props with the image's natural dimensions when it finishes loading.
   * This does not mean width and height props are redundant; they are still used as placeholders while scrolling.
   */
  naturalSize?: boolean

  /**
   * True to center this element horizontally.
   * This works by setting margins to 'auto'.
   */
  centered?: boolean

  /** True to add rounded corners to this image. */
  rounded?: boolean | number | string

  /** Image styling. */
  style?: CSSProperties

  /** Styling of the wrapper element. */
  wrapperStyle?: CSSProperties

  /** Set to true to prevent this image from displaying completely. */
  disabled?: boolean

  /** Whether loading animation should be disabled or not. */
  loadingDisabled?: boolean

  /**
   * Scale value by which window's width and height will be multiplied.
   * When this element reaches near the visible window viewport, it will begin loading.
   * Defaults to 2.
   */
  preloadScale?: number

  /** Children to insert into the wrapper div, above the image and loading animation. */
  children?: ReactNode
}, ref: Ref<HTMLImageElement>) => { // tslint:disable-line: align
  const rerender = useUpdate()

  let [visible, setVisible] = useState(false)
  const [shouldLoad, setShouldLoad] = useState(false)

  visible = visible && !disabled

  const { value: result, error } = useAsync(async () => {
    if (!shouldLoad) {
      visible && setShouldLoad(true)
      return
    }

    try {
      let blob: Blob

      if (typeof src === 'function')
        blob = await src()
      else
        blob = src

      await onLoad?.(blob)

      return URL.createObjectURL(blob)
    }
    catch (e) {
      console.log('error caught by async image', e)
      throw e
    }
  }, [shouldLoad])

  // revoke image reference on unmount
  useLayoutEffect(() => () => { result && URL.revokeObjectURL(result) }, [result])

  const imageRef = useRef<HTMLImageElement>(null)

  // poll image's natural size because onLoad tends to be unreliable
  useInterval(() => {
    if (imageRef.current?.naturalWidth)
      console.log('natural size loaded', imageRef.current.naturalHeight)

    rerender()
  }, visible && naturalSize && !imageRef.current?.naturalWidth ? 0 : null)

  if (naturalSize && imageRef.current?.naturalWidth) {
    width = imageRef.current.naturalWidth
    height = imageRef.current.naturalHeight
  }

  fluid = fluid && !!width && !!height

  // styling for wrapper
  wrapperStyle = {
    position: 'relative',
    width: fluid ? '100%' : (width || 'auto'),
    height: fluid ? 0 : (height || 'auto'),
    paddingTop: fluid ? height! / width! * 100 + '%' : undefined,

    display: 'block',
    margin: centered ? 'auto' : undefined,
    overflow: 'hidden',

    ...wrapperStyle
  }

  // styling for content inside wrapper
  style = {
    position: 'absolute',
    top: 0,
    left: 0,
    width: '100%',
    height: '100%',

    display: visible ? undefined : 'none', // use css visiblity instead of removing from the dom for performance
    objectFit: resize === 'fill' ? 'cover' : 'contain',
    borderRadius: typeof rounded === 'boolean' ? '0.2em' : rounded,

    ...style
  }

  const { width: windowWidth, height: windowHeight } = useContext(LayoutContext)

  return <VisibilitySensor
    onChange={v => {
      if (v) {
        setVisible(v)
        !shouldLoad && setShouldLoad(true)
      }

      onVisibleChange?.(v)
    }}
    intervalCheck
    scrollCheck
    resizeCheck
    partialVisibility
    offset={{
      top: -windowHeight * preloadScale,
      bottom: -windowHeight * preloadScale,
      left: -windowWidth * preloadScale,
      right: -windowWidth * preloadScale
    }}>

    <div ref={wrapperRef} style={wrapperStyle}>
      {result &&
        <img
          ref={composeRefs(imageRef, ref)}
          src={result}
          alt={result}
          style={style} />}

      {visible && !result && !error && !loadingDisabled &&
        <Spin style={{
          position: 'absolute',
          left: '50%',
          top: '50%',
          transform: 'translate(-50%, -50%)'
        }} />}

      {children}
    </div>
  </VisibilitySensor>
})
