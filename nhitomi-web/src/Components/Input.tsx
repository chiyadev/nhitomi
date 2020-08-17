import React, { Dispatch, useState, ReactNode, useMemo, KeyboardEvent, useRef } from 'react'
import { useSpring, animated } from 'react-spring'
import { convertHex } from '../theme'
import { colors } from '../theme.json'
import { cx, css } from 'emotion'
import { CloseOutlined } from '@ant-design/icons'
import { useShortcut } from '../shortcut'

export type InputStatus = 'none' | 'success' | 'error' | 'warning'

export const Input = ({ value, setValue, type = 'input', autoFocus, placeholder, allowClear, className, onSubmit, onKeyDown, status = { status: 'none' } }: {
  value: string
  setValue: Dispatch<string>
  type?: 'input' | 'textarea'
  autoFocus?: boolean
  placeholder?: ReactNode
  allowClear?: boolean
  className?: string
  onSubmit?: (value: string) => void
  onKeyDown?: (e: KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => void
  status?: { status: InputStatus, help?: ReactNode }
}) => {
  const ref = useRef<HTMLInputElement & HTMLTextAreaElement>(null)

  useShortcut('cancelKey', () => ref.current?.blur(), ref)

  const [hover, setHover] = useState(false)
  const [focus, setFocus] = useState(false)

  let color: string

  switch (status.status) {
    case 'none': color = colors.gray[500]; break
    case 'success': color = colors.green[500]; break
    case 'error': color = colors.red[500]; break
    case 'warning': color = colors.orange[500]; break
  }

  const inputStyle = useSpring({
    borderColor: convertHex(color, focus || hover ? 0.3 : 0.15),
    backgroundColor: convertHex(color, focus ? 0.2 : 0.1)
  })

  const placeholderStyle = useSpring({
    color: convertHex(color, focus ? 0.75 : 0.5)
  })

  const helpStyle = useSpring({
    color,
    opacity: status.help ? 1 : 0
  })

  const [clearHover, setClearHover] = useState(false)
  const clearVisible = allowClear && value && !focus
  const clearStyle = useSpring(clearVisible
    ? {
      opacity: clearHover ? 0.75 : 0.5,
      transform: clearHover ? 'scale(1.1)' : 'scale(1)'
    } : {
      opacity: 0,
      transform: 'scale(1)'
    })

  const input = useMemo(() => {
    switch (type) {
      case 'input':
        return (
          <input
            ref={ref}
            className={cx('px-2 py-1 w-full', css`
              background: transparent;

              &::selection {
                background: ${colors.blue[600]};
              }
            `)}
            value={value}
            autoFocus={autoFocus}
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
            ref={ref}
            className={cx('px-2 py-1 w-full', css`
              background: transparent;
              min-height: 3em;

              &::selection {
                background: ${colors.blue[600]};
              }
            `)}
            value={value}
            autoFocus={autoFocus}
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
  }, [autoFocus, onKeyDown, onSubmit, setValue, type, value])

  return (
    <div className={cx('inline-block text-white', className)}>
      <animated.div className='w-full relative rounded-sm overflow-hidden border' style={inputStyle}>
        {input}

        {!value && (
          <animated.div
            style={placeholderStyle}
            className='absolute top-0 left-0 w-full px-2 py-1 align-top pointer-events-none truncate'
            children={placeholder} />
        )}

        {allowClear && (
          <animated.div
            style={clearStyle}
            className={cx('absolute top-0 right-0 px-2 py-1 cursor-pointer h-8', { 'pointer-events-none': !clearVisible })}
            onMouseEnter={() => setClearHover(true)}
            onMouseLeave={() => setClearHover(false)}
            onMouseDown={() => { setValue(''); setTimeout(() => ref.current?.focus()) }}>

            <CloseOutlined className='text-xs' />
          </animated.div>
        )}
      </animated.div>

      {status.help && (
        <animated.div style={helpStyle} className='text-xs truncate mt-1' children={status.help} />
      )}
    </div>
  )
}
