import React from 'react'
import { cx, css } from 'emotion'
import { ReadFilled, FolderOutlined, InfoCircleOutlined, HeartOutlined, SettingOutlined, BookOutlined, SettingFilled, FolderOpenFilled, InfoCircleFilled } from '@ant-design/icons'
import { RoundIconButton } from '../Components/RoundIconButton'
import { Tooltip } from '../Components/Tooltip'
import { FormattedMessage } from 'react-intl'
import { BookListingLink } from '../BookListing'
import { SettingsLink } from '../Settings'
import { Route, Switch } from 'react-router-dom'
import { useSpring, animated } from 'react-spring'
import { SelfCollectionListingLink } from '../CollectionListing'
import { useClientInfo } from '../ClientManager'
import { Disableable } from '../Components/Disableable'

export const StripWidth = 64

export const Strip = () => {
  const { info } = useClientInfo()

  const style = useSpring({
    from: { opacity: 0, marginLeft: -5 },
    to: { opacity: 1, marginLeft: 0 }
  })

  return (
    <Disableable disabled={!info.authenticated}>
      <animated.div
        style={style}
        className={cx('fixed top-0 left-0 bottom-0 z-10 text-white py-4 flex flex-col items-center select-none', css`width: ${StripWidth}px;`)}>

        <Tooltip overlay={<span><FormattedMessage id='pages.home.title' /> <HeartOutlined /></span>} placement='right' className='mb-4'>
          <BookListingLink>
            <img alt='logo' className='w-10 h-10 pointer-events-none' src='/logo-40x40.png' />
          </BookListingLink>
        </Tooltip>

        <Buttons />
      </animated.div>
    </Disableable>
  )
}

const Buttons = () => {
  return <>
    <Tooltip overlay={<FormattedMessage id='pages.bookListing.title' />} placement='right'>
      <BookListingLink>
        <RoundIconButton>
          <Switch>
            <Route path='/books'><ReadFilled /></Route>
            <Route><BookOutlined /></Route>
          </Switch>
        </RoundIconButton>
      </BookListingLink>
    </Tooltip>

    <Tooltip overlay={<FormattedMessage id='pages.collectionListing.title' />} placement='right'>
      <SelfCollectionListingLink>
        <RoundIconButton>
          <Switch>
            <Route path='/collections'><FolderOpenFilled /></Route>
            <Route path='/users/:id/collections'><FolderOpenFilled /></Route>
            <Route><FolderOutlined /></Route>
          </Switch>
        </RoundIconButton>
      </SelfCollectionListingLink>
    </Tooltip>

    <Tooltip overlay={<FormattedMessage id='pages.settings.title' />} placement='right'>
      <SettingsLink>
        <RoundIconButton>
          <Switch>
            <Route path='/settings'><SettingFilled /></Route>
            <Route><SettingOutlined /></Route>
          </Switch>
        </RoundIconButton>
      </SettingsLink>
    </Tooltip>

    <Tooltip overlay={<FormattedMessage id='pages.about.title' />} placement='right'>
      <RoundIconButton>
        <Switch>
          <Route path='/about'><InfoCircleFilled /></Route>
          <Route><InfoCircleOutlined /></Route>
        </Switch>
      </RoundIconButton>
    </Tooltip>
  </>
}
