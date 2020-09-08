import React, { ReactNode } from 'react'
import { useLayout } from '../LayoutManager'
import { animated, useSpring } from 'react-spring'
import { css, cx } from 'emotion'

export const SupportDescription = () => (
  <div className='space-y-2'>
    <div>nhitomi is a free service, but serving thousands of visitors everyday and maintaining our infrastructure is costly.</div>
    <div>We are an open-source project and do not rely on any advertisements.</div>
    <div>Please help us keep going.</div>
  </div>
)

export const MainCard = ({ children }: { children?: ReactNode }) => {
  const { screen } = useLayout()

  const imageStyle = useSpring({
    from: { transform: 'translateY(5px)' },
    to: { transform: 'translateY(0)' }
  })

  switch (screen) {
    case 'sm':
      return (
        <div className='flex flex-col'>
          <animated.div style={imageStyle} className={cx('relative overflow-hidden', css`height: 300px;`)}>
            <img
              alt='megumi'
              src='/assets/images/megumi_happy.png'
              className={cx('select-none pointer-events-none rounded absolute w-full max-w-xs', css`
                top: 50%;
                transform: translateY(-50%);
              `)} />
          </animated.div>

          <div className='bg-white text-black rounded-lg p-4 z-10 shadow-lg' children={children} />
        </div>
      )

    case 'lg':
      return (
        <div className={cx('relative', css`height: 350px;`)}>
          <animated.img
            style={imageStyle}
            alt='megumi'
            src='/assets/images/megumi_happy.png'
            className='select-none pointer-events-none object-cover rounded absolute ml-8 w-64 h-full z-10' />

          <div className='bg-white text-black rounded-lg absolute transform-center w-full pl-64 shadow-lg'>
            <div className='ml-8 px-4 py-8' children={children} />
          </div>
        </div>
      )
  }
}
