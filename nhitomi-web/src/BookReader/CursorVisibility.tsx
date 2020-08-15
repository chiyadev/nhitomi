import React, { ReactNode, useState, useLayoutEffect, useRef } from 'react'
import { CurrentPage } from './ScrollManager'
import { usePageState } from '../state'

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
      style={{ cursor: visible ? undefined : 'none' }}
      onMouseMove={() => setVisible(true)}
      className={className}
      children={children} />
  )
}
