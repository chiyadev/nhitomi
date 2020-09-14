import React from 'react'
import { useSpring, animated } from 'react-spring'
import { useClientInfo } from './ClientManager'
import { FormattedMessage } from 'react-intl'
import { WarningFilled } from '@ant-design/icons'

export const MaintenanceHeader = () => {
  const { info: { maintenance } } = useClientInfo()

  if (!maintenance)
    return null

  return (
    <Inner />
  )
}

const Inner = () => {
  const style = useSpring({
    from: { marginTop: -5, opacity: 0 },
    to: { marginTop: 0, opacity: 1 }
  })

  return (
    <animated.div
      style={style}
      className='w-full px-4 py-2 text-sm bg-red-darkest text-white rounded-b'>

      <WarningFilled />
      {' '}
      <FormattedMessage id='components.maintenanceHeader.text' />
    </animated.div>
  )
}
