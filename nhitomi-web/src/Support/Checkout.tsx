import React, { useState } from 'react'
import { Slider } from '../Components/Slider'
import { usePageState } from '../state'
import { getColor } from '../theme'
import { FormattedMessage } from 'react-intl'
import { animated, useSpring } from 'react-spring'
import { cx, css } from 'emotion'

const monthPrice = 3

export const Checkout = () => {
  const [amount, setAmount] = usePageState('amount', monthPrice)
  const duration = Math.floor(amount / monthPrice)

  return (
    <div className='max-w-sm mx-auto p-4 space-y-4'>
      <CheckoutButton duration={duration} />

      <div className='space-y-1'>
        <div className='text-center text-xs'>{amount} USD</div>

        <Slider
          className='w-full'
          color={getColor('pink')}
          min={0}
          max={12}
          value={amount / monthPrice}
          setValue={v => setAmount(Math.max(1, v) * monthPrice)}
          overlay={`${amount} USD`} />
      </div>
    </div>
  )
}

const CheckoutButton = ({ duration }: { duration: number }) => {
  const [hover, setHover] = useState(false)
  const imageStyle = useSpring({
    opacity: hover ? 0.6 : 0.5,
    transform: `translate(-50%, -50%) scale(${hover ? 1.1 : 1})`
  })

  return (
    <div
      className='mx-auto w-64 h-32 bg-black rounded-lg relative overflow-hidden cursor-pointer'
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      <animated.img
        style={imageStyle}
        alt='buttonbg'
        src='/assets/images/megumi_button_bg.jpg'
        className={cx('absolute w-full object-cover', css`left: 50%; top: 50%;`)} />

      <div className='absolute transform-center w-full text-center'>
        <div className='text-xl'>
          <FormattedMessage id='pages.support.buy' />
        </div>
        <div className='text-xs'>
          <FormattedMessage id='pages.support.duration' values={{ duration }} />
        </div>
      </div>
    </div>
  )
}
