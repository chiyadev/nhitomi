import React, { ReactNode, useState, useLayoutEffect, useRef } from 'react'
import { CurrentPage } from './ScrollManager'
import { usePageState } from '../state'
import { cx, css } from 'emotion'

export const CursorVisibility = ({ children, className }: { children?: ReactNode, className?: string }) => {
  const [visible, setVisible] = useState(true)

  const [currentPage] = usePageState<CurrentPage>('page', { rowPassive: 0, pagePassive: 0 })
  const lastPage = useRef(currentPage)

  useLayoutEffect(() => {
    if (currentPage.pagePassive !== lastPage.current.pagePassive)
      setVisible(false)

    lastPage.current = currentPage
  }, [currentPage])

  return (
    <div
      onMouseMove={() => setVisible(true)}
      className={cx(className, css`
        cursor: ${visible ? 'inherit' : 'none'};
      `)}
      children={children} />
  )
}
