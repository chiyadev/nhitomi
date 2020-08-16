import React, { Dispatch, ReactNode, useState } from 'react'
import { cx } from 'emotion'
import { CheckOutlined } from '@ant-design/icons'
import { colors } from '../theme.json'
import { useSpring, animated } from 'react-spring'
import { convertHex } from '../theme'

export type CheckBoxType = 'check' | 'radio'

export const CheckBox = ({ value, setValue, type = 'check', children, className }: {
  value?: boolean
  setValue?: Dispatch<boolean>
  type?: CheckBoxType
  children?: ReactNode
  className?: string
}) => {
  const [hover, setHover] = useState(false)

  return (
    <div
      className={cx('flex flex-row items-center', className)}
      onClick={() => setValue?.(!value)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      <div className='mx-2 my-1'>
        {type === 'check' ? <Check value={value} hover={hover} />
          : type === 'radio' ? <Radio value={value} hover={hover} />
            : null}
      </div>

      <div className='flex-1 mr-2' children={children} />
    </div>
  )
}

const Check = ({ value, hover }: { value?: boolean, hover?: boolean }) => {
  const boxStyle = useSpring({
    backgroundColor: value ? colors.blue[600] : convertHex('#000', 0),
    borderColor: value ? convertHex('#000', 0) : convertHex(colors.gray[600], hover ? 0.8 : 0.4)
  })

  const checkStyle = useSpring({
    opacity: value ? 1 : 0,
    marginBottom: value ? 0 : -2,
    transform: value ? 'scale(1)' : 'scale(0.8)'
  })

  return (
    <animated.div
      style={boxStyle}
      className='w-4 h-4 flex items-center text-center rounded border box-content'>

      <animated.span
        style={checkStyle}
        className='flex-1 text-xs'>

        <CheckOutlined />
      </animated.span>
    </animated.div>
  )
}

const Radio = ({ value, hover }: { value?: boolean, hover?: boolean }) => {
  const boxStyle = useSpring({
    borderColor: value ? colors.blue[600] : convertHex(colors.gray[600], hover ? 0.8 : 0.4)
  })

  const dotStyle = useSpring({
    opacity: value ? 1 : 0,
    transform: value ? 'scale(0.6)' : 'scale(0)'
  })

  return (
    <animated.div
      style={boxStyle}
      className='w-4 h-4 flex items-center justify-center rounded-full border box-content'>

      <animated.span
        style={dotStyle}
        className='w-4 h-4 bg-blue-600 rounded-full' />
    </animated.div>
  )
}
