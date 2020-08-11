import React from 'react'
import { FormattedDate, FormattedTime } from 'react-intl'
import { Tooltip } from './Tooltip'

export const TimeDisplay = ({ value, className }: { value: Date, className?: string }) => {
  return (
    <Tooltip
      className={className}
      placement='top'
      overlay={value.toUTCString()}>

      <FormattedDate value={value} />
      {' '}
      <FormattedTime value={value} />
    </Tooltip>
  )
}
