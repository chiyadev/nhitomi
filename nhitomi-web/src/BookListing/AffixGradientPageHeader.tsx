import React, { ReactNode, useState } from 'react'
import { Affix } from 'antd'
import { presetPrimaryColors } from '@ant-design/colors'

const bg = presetPrimaryColors['@component-background']

export const AffixGradientPageHeader = ({ children }: { children?: ReactNode }) => {
  const [affixed, setAffixed] = useState(false)
  const [hovered, setHovered] = useState(false)

  return (
    <div
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}>

      <Affix onChange={v => setAffixed(v || false)}>
        <div children={children} style={{
          background: !affixed || hovered ? `linear-gradient(${bg} 0%, ${bg} 100%)` : `linear-gradient(${bg} 0%, transparent 100%)`,
          opacity: affixed && !hovered ? 0.8 : 1,
          transition: 'background 0.2s, opacity 0.2s'
        }} />
      </Affix>
    </div>
  )
}
