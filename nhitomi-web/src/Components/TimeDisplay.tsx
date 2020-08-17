import React from 'react'
import { FormattedDate, FormattedTime } from 'react-intl'
import { Tooltip } from './Tooltip'
import { cx } from 'emotion'

export const TimeDisplay = ({ value, className }: { value: Date, className?: string }) => {
  return (
    <Tooltip
      className={cx('inline-flex', className)}
      placement='top'
      overlay={value.toUTCString()}>

      <span className='cursor-default'>
        <FormattedDate value={value} />
        {' '}
        <FormattedTime value={value} />
      </span>
    </Tooltip>
  )
}
