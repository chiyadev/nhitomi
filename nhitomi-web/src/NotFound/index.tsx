import React from 'react'
import { PageContainer } from '../Components/PageContainer'
import { FormattedMessage } from 'react-intl'
import { RoundIconButton } from '../Components/RoundIconButton'
import { HomeOutlined, LeftOutlined } from '@ant-design/icons'
import { BookListingLink } from '../BookListing'
import { Tooltip } from '../Components/Tooltip'
import { BackLink } from '../Prefetch'

export const NotFound = () => {
  return (
    <PageContainer className='flex flex-col justify-center p-4 space-y-4'>
      <img className='mx-auto rounded max-w-full pointer-events-none select-none' alt='404' src='/assets/statuses/megu404.jpg' style={{ maxHeight: '50vh' }} />

      <div className='text-center'>
        <div className='text-4xl font-bold'>404</div>
        <div className='text-sm text-gray-darker'><FormattedMessage id='pages.notFound.description' /></div>
      </div>

      <div className='flex flex-row justify-center'>
        <Tooltip placement='bottom' overlay={<FormattedMessage id='pages.notFound.back' />}>
          <BackLink>
            <RoundIconButton>
              <LeftOutlined />
            </RoundIconButton>
          </BackLink>
        </Tooltip>

        <Tooltip placement='bottom' overlay={<FormattedMessage id='pages.notFound.home' />}>
          <BookListingLink>
            <RoundIconButton>
              <HomeOutlined />
            </RoundIconButton>
          </BookListingLink>
        </Tooltip>
      </div>
    </PageContainer>
  )
}
