import React, { useLayoutEffect, useRef, ComponentProps } from 'react'
import { SettingsFocus } from '.'
import { useQueryState } from '../state'
import { cx, css } from 'emotion'

export const SettingsFocusContainer = ({ focus, className, ...props }: ComponentProps<'div'> & { focus: SettingsFocus }) => {
  const ref = useRef<HTMLDivElement>(null)
  const [currentFocus] = useQueryState<SettingsFocus>('replace', 'focus')

  useLayoutEffect(() => {
    if (currentFocus === focus) {
      ref.current?.scrollIntoView()
    }
  }, [currentFocus, focus, ref])

  return (
    <div
      ref={ref}
      className={cx(className, { 'rounded border border-blue-500 border-opacity-50 p-2 -m-2': currentFocus === focus }, css`scroll-margin: 0.5em;`)}

      {...props} />
  )
}
