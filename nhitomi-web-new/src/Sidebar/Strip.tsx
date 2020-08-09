import React from 'react'
import { cx, css } from 'emotion'
import { ReadFilled, FolderOutlined, InfoCircleOutlined, HeartOutlined, SettingOutlined, BookOutlined, SettingFilled, FolderOpenFilled, InfoCircleFilled } from '@ant-design/icons'
import { RoundIconButton } from '../Components/RoundIconButton'
import { Tooltip } from '../Components/Tooltip'
import { FormattedMessage } from 'react-intl'
import { BookListingLink } from '../BookListing'
import { SettingsLink } from '../Settings'
import { Route, Switch } from 'react-router-dom'

export const StripWidth = 64

export const Strip = () => {
  return (
    <div className={cx('fixed top-0 left-0 bottom-0 z-10 bg-black text-white py-4 flex flex-col items-center', css`width: ${StripWidth}px;`)}>
      <ul>
        <li className='mb-4'>
          <Tooltip overlay={<span><FormattedMessage id='pages.home.title' /> <HeartOutlined className='align-middle' /></span>} placement='right'>
            <BookListingLink>
              <img alt='logo' className='w-10 h-10' src='/logo-40x40.png' />
            </BookListingLink>
          </Tooltip>
        </li>

        <li>
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
        </li>

        <li>
          <Tooltip overlay={<FormattedMessage id='pages.collectionListing.title' />} placement='right'>
            <RoundIconButton>
              <Switch>
                <Route path='/collections'><FolderOpenFilled /></Route>
                <Route><FolderOutlined /></Route>
              </Switch>
            </RoundIconButton>
          </Tooltip>
        </li>

        <li>
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
        </li>

        <li>
          <Tooltip overlay={<FormattedMessage id='pages.about.title' />} placement='right'>
            <RoundIconButton>
              <Switch>
                <Route path='/about'><InfoCircleFilled /></Route>
                <Route><InfoCircleOutlined /></Route>
              </Switch>
            </RoundIconButton>
          </Tooltip>
        </li>
      </ul>
    </div>
  )
}
