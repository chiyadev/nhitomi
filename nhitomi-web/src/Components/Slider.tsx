import React, { Dispatch, ReactNode } from 'react'
import RcSlider, { Handle as RcHandle } from 'rc-slider'
import { getColor, Color } from '../theme'
import { Tooltip } from './Tooltip'

import 'rc-slider/assets/index.css'

export const Slider = ({ min, max, value, setValue, color = getColor('blue'), railColor = getColor('gray').opacity(0.5), className, overlay }: {
  min?: number
  max?: number
  value: number
  setValue: Dispatch<number>
  color?: Color
  railColor?: Color
  className?: string
  overlay?: ReactNode
}) => {
  return (
    <RcSlider
      min={min}
      max={max}
      value={value}
      onChange={setValue}
      className={className}
      handle={props => {
        const { index, dragging } = props

        return (
          <Tooltip
            key={index}
            overlay={overlay}
            visible={!!overlay && dragging}
            placement='top'>

            <RcHandle {...props} />
          </Tooltip>
        )
      }}
      railStyle={{
        backgroundColor: railColor.rgb,
        height: '0.25em'
      }}
      trackStyle={{
        backgroundColor: color.rgb,
        height: '0.25em'
      }}
      handleStyle={{
        width: '1em',
        height: '1em',
        borderColor: color.rgb,
        borderWidth: 1,
        backgroundColor: getColor('white').rgb
      }} />
  )
}
