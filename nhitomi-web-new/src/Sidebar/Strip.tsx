import React from 'react'
import styled from '@emotion/styled'
import { ReadFilled, FolderOutlined, InfoCircleOutlined } from '@ant-design/icons'
import { RoundIconButton } from '../Components/RoundIconButton'

export const StripWidth = 64

const Container = styled.div`
  width: ${StripWidth}px;
`

export const Strip = () => {
  return (
    <Container className='h-screen pt-4 pb-4'>
      <ul>
        <li>
          <img alt='logo' className='mx-auto mb-4 w-10 h-10' src='/logo-40x40.png' />
        </li>

        <li>
          <RoundIconButton className='mx-auto' tooltip={{ overlay: 'Books', placement: 'right' }}>
            <ReadFilled />
          </RoundIconButton>
        </li>

        <li>
          <RoundIconButton className='mx-auto' tooltip={{ overlay: 'Collections', placement: 'right' }}>
            <FolderOutlined />
          </RoundIconButton>
        </li>

        <li>
          <RoundIconButton className='mx-auto' tooltip={{ overlay: 'About', placement: 'right' }}>
            <InfoCircleOutlined />
          </RoundIconButton>
        </li>
      </ul>
    </Container>
  )
}
