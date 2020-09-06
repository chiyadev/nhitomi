import React from 'react'
import { SettingsFocusContainer } from './SettingsFocusContainer'
import { Link } from 'react-router-dom'
import { LinkOutlined } from '@ant-design/icons'

export const Debug = () => {
  return (
    <SettingsFocusContainer focus='debug'>
      <div className='text-base'>Debug</div>

      <Link to='/settings/debug' className='text-xs text-blue'><LinkOutlined /> Debug helper</Link>
    </SettingsFocusContainer>
  )
}
