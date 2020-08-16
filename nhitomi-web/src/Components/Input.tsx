import React, { Dispatch, useState, ReactNode, useMemo, KeyboardEvent } from 'react'
import { useSpring, animated } from 'react-spring'
import { convertHex } from '../theme'
import { colors } from '../theme.json'
import { cx, css } from 'emotion'

export type InputStatus = 'none' | 'success' | 'error' | 'warning'

export const Input = ({ value, setValue, type = 'input', placeholder, className, onSubmit, onKeyDown, status = { status: 'none' } }: {
  value: string
  setValue: Dispatch<string>
  type?: 'input' | 'textarea'
  placeholder?: ReactNode
  className?: string
  onSubmit?: (value: string) => void
  onKeyDown?: (e: KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => void
  status?: { status: InputStatus, help?: ReactNode }
}) => {
  const [hover, setHover] = useState(false)
  const [focus, setFocus] = useState(false)

  let color: string

  switch (status.status) {
    case 'none': color = colors.gray[500]; break
    case 'success': color = colors.green[500]; break
    case 'error': color = colors.red[500]; break
    case 'warning': color = colors.orange[500]; break
  }

  const style = useSpring({
    borderColor: convertHex(color, focus || hover ? 0.3 : 0.15),
    backgroundColor: convertHex(color, focus ? 0.2 : 0.1)
  })

  const placeholderStyle = useSpring({
    color: convertHex(color, focus ? 0.75 : 0.5)
  })

  const helpStyle = useSpring({
    color,
    opacity: typeof status.help === 'undefined' ? 0 : 1
  })

  const input = useMemo(() => {
    switch (type) {
      case 'input':
        return (
          <input
            className={cx('px-2 py-1 w-full', css`
              background: transparent;

              &::selection {
                background: ${colors.blue[600]};
              }
            `)}
            value={value}
            onChange={({ target: { value } }) => setValue(value)}
            onMouseEnter={() => setHover(true)}
            onMouseLeave={() => setHover(false)}
            onFocus={() => setFocus(true)}
            onBlur={() => setFocus(false)}
            onKeyDown={e => {
              // enter
              if (onSubmit && e.keyCode === 13) {
                onSubmit(value)
                e.preventDefault()
              }

              onKeyDown?.(e)
            }} />
        )

      case 'textarea':
        return (
          <textarea
            className={cx('px-2 py-1 w-full', css`
              background: transparent;
              min-height: 3em;

              &::selection {
                background: ${colors.blue[600]};
              }
            `)}
            value={value}
            onChange={({ target: { value } }) => setValue(value)}
            onMouseEnter={() => setHover(true)}
            onMouseLeave={() => setHover(false)}
            onFocus={() => setFocus(true)}
            onBlur={() => setFocus(false)}
            onKeyDown={e => {
              // enter
              if (onSubmit && e.keyCode === 13 && e.ctrlKey) {
                onSubmit(value)
                e.preventDefault()
              }

              onKeyDown?.(e)
            }} />
        )
    }
  }, [onKeyDown, onSubmit, setValue, type, value])

  return (
    <div className={cx('inline-block text-white', className)}>
      <animated.div className='w-full relative rounded-sm overflow-hidden border' style={style}>
        {input}

        {!value && (
          <animated.div
            style={placeholderStyle}
            className='absolute top-0 left-0 w-full h-full px-2 py-1 align-top pointer-events-none truncate'
            children={placeholder} />
        )}
      </animated.div>

      {typeof status.help !== 'undefined' && (
        <animated.div style={helpStyle} className='text-xs truncate' children={status.help} />
      )}
    </div>
  )
}
