import React, { useState } from 'react'
import { Affix, PageHeader } from 'antd'
import { BookOutlined } from '@ant-design/icons'
import { Search } from './Search'

export const Header = () => {
  const [affixed, setAffixed] = useState(false)
  const [hovered, setHovered] = useState(false)

  return (
    <div
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}>

      <Affix onChange={v => setAffixed(v || false)}>
        <PageHeader
          style={{
            background: !affixed || hovered ? 'linear-gradient(black 0%, black 100%)' : 'linear-gradient(black 0%, transparent 100%)',
            opacity: affixed && !hovered ? 0.8 : 1,
            transition: 'background 0.2s, opacity 0.2s'
          }}
          avatar={{ icon: <BookOutlined />, shape: 'square' }}
          title='Books'
          subTitle='List of all books'
          extra={<Search />} />
      </Affix>
    </div>
  )
}
