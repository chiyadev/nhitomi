import React from 'react'
import styled from '@emotion/styled'
import { ReadFilled, FolderOutlined, InfoCircleOutlined } from '@ant-design/icons'
import { RoundIconButton } from '../Components/RoundIconButton'
import { Tooltip } from '../Components/Tooltip'
import { FormattedMessage } from 'react-intl'

export const StripWidth = 64

const Container = styled.div`
  width: ${StripWidth}px;
`

export const Strip = () => {
  return (
    <Container className='h-screen pt-4 pb-4 flex flex-col items-center'>
      <ul>
        <li className='mb-4'>
          <Tooltip overlay={<FormattedMessage id='pages.home.title' />} placement='right'>
            <img alt='logo' className='w-10 h-10' src='/logo-40x40.png' />
          </Tooltip>
        </li>

        <li>
          <Tooltip overlay={<FormattedMessage id='pages.bookListing.title' />} placement='right'>
            <RoundIconButton>
              <ReadFilled />
            </RoundIconButton>
          </Tooltip>
        </li>

        <li>
          <Tooltip overlay={<FormattedMessage id='pages.collectionListing.title' />} placement='right'>
            <RoundIconButton>
              <FolderOutlined />
            </RoundIconButton>
          </Tooltip>
        </li>

        <li>
          <Tooltip overlay={<FormattedMessage id='pages.about.title' />} placement='right'>
            <RoundIconButton>
              <InfoCircleOutlined />
            </RoundIconButton>
          </Tooltip>
        </li>
      </ul>
    </Container>
  )
}
