import { useConfig } from '../ConfigManager'
import { useLayoutEffect } from 'react'
import { useLayout } from '../LayoutManager'

export const LayoutSetter = () => {
  const { screen } = useLayout()

  const [, setImagesPerRow] = useConfig('bookReaderImagesPerRow')
  const [, setViewportBound] = useConfig('bookReaderViewportBound')

  // automatically adjust layout for mobile
  useLayoutEffect(() => {
    switch (screen) {
      case 'sm':
        setImagesPerRow(1)
        setViewportBound(false)
        break

      case 'lg':
        setImagesPerRow(2)
        setViewportBound(true)
        break
    }
  }, [screen, setImagesPerRow, setViewportBound])

  return null
}
