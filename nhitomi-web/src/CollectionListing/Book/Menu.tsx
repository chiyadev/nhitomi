import React from 'react'
import { Tooltip } from '../../Components/Tooltip'
import { FormattedMessage } from 'react-intl'
import { ObjectType } from 'nhitomi-api'
import { CollectionCreateLink } from '../Create'
import { PlusOutlined } from '@ant-design/icons'
import { RoundIconButton } from '../../Components/RoundIconButton'

export const Menu = () => <>
  <NewButton />
</>

const NewButton = () => (
  <Tooltip placement='bottom' overlay={<FormattedMessage id='pages.collectionListing.create.title' />}>
    <CollectionCreateLink type={ObjectType.Book}>
      <RoundIconButton>
        <PlusOutlined />
      </RoundIconButton>
    </CollectionCreateLink>
  </Tooltip>
)
