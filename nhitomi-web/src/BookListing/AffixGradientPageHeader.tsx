import React, { ReactNode, useState } from 'react'
import { Affix } from 'antd'

export const AffixGradientPageHeader = ({ children }: { children?: ReactNode }) => {
  const [affixed, setAffixed] = useState(false)
  const [hovered, setHovered] = useState(false)

  return (
    <div
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}>

      <Affix onChange={v => setAffixed(v || false)}>
        <div children={children} style={{
          background: !affixed || hovered ? 'linear-gradient(black 0%, black 100%)' : 'linear-gradient(black 0%, transparent 100%)',
          opacity: affixed && !hovered ? 0.8 : 1,
          transition: 'background 0.2s, opacity 0.2s'
        }} />
      </Affix>
    </div>
  )
}
