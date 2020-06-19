import React from 'react'
import { useIntl } from 'react-intl'
import { Tooltip } from 'antd'

export const TimeDisplay = ({ time }: { time: Date }) => {
  const { formatDate, formatTime } = useIntl()

  return <Tooltip title={time.toString()}>
    <label>{formatDate(time)} {formatTime(time)}</label>
  </Tooltip>
}
