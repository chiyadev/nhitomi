import { Layout, Menu, PageHeader, Button } from 'antd'
import React, { useContext, useLayoutEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { useShortcut } from './shortcuts'
import { NotificationContext } from './NotificationContext'
import { ClientContext } from './ClientContext'
import { LayoutContext } from './LayoutContext'
import { BarsOutlined, ReadOutlined, InfoCircleOutlined, FolderOpenOutlined, BookOutlined, FolderOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { BookListingLink } from './BookListing'
import { AboutLink } from './About'
import { CollectionListingLink } from './CollectionListing'
import { ShortcutKeyTooltip } from './ShortcutKeyTooltip'

export const SideBarWidth = 200

export const SideBar = () => {
  const { pathname } = useLocation()
  const client = useContext(ClientContext)
  const { alert } = useContext(NotificationContext)
  const { sidebar, setSidebar, mobile } = useContext(LayoutContext)

  // collapse on url change if mobile
  // we use setTimeout to avoid a bug in antd where menu shows a tooltip that can't be disabled
  useLayoutEffect(() => { mobile && sidebar && setTimeout(() => setSidebar(false), 100) }, [pathname]) // eslint-disable-line

  // collapse on shortcut
  useShortcut('sidebarKey', () => {
    setSidebar(!sidebar)

    if (sidebar)
      alert.info(<FormattedMessage id='sidebar.collapsed' />)
    else
      alert.info(<FormattedMessage id='sidebar.opened' />)
  })

  const selected = pathname.split('/')[1]

  return <Layout.Sider
    theme='dark'
    breakpoint='md'
    onBreakpoint={v => setSidebar(!v)}
    width={SideBarWidth}
    collapsible
    collapsed={!sidebar}
    collapsedWidth={0}
    onCollapse={v => setSidebar(!v)}
    trigger={null}
    style={{
      position: 'fixed',
      height: '100vh',
      zIndex: 100,
      top: 0,
      left: 0,
      boxShadow: sidebar ? '-3px 0 6px 0 #555' : undefined
    }}>

    <ShortcutKeyTooltip
      shortcut='sidebarKey'
      className='ant-layout-sider-zero-width-trigger ant-layout-sider-zero-width-trigger-left'
      placement={sidebar ? 'left' : 'right'}>

      <BarsOutlined
        style={{
          lineHeight: '46px' // this is a hack
        }}
        onClick={() => setSidebar(!sidebar)} />
    </ShortcutKeyTooltip>

    <div style={{
      opacity: sidebar ? 1 : 0,
      transition: sidebar ? 'opacity 0.5s' : undefined
    }}>
      <BookListingLink>
        <PageHeader
          backIcon={false}
          style={{ minWidth: SideBarWidth, marginLeft: -8 }}
          title={<span style={{ display: 'flex' }}>
            <img alt='logo' src='/favicon-32x32.png' style={{ flex: 1, marginRight: 6 }} />
            <span>nhitomi</span>
          </span>} />
      </BookListingLink>

      <Menu theme='dark' mode='inline' selectedKeys={[selected]}>
        <Menu.Item key='books'>
          <BookListingLink>
            {selected === 'books' ? <ReadOutlined /> : <BookOutlined />}
            <FormattedMessage id='bookListing.header.title' />
          </BookListingLink>
        </Menu.Item>

        <Menu.Item key='collections'>
          <CollectionListingLink>
            {selected === 'collections' ? <FolderOpenOutlined /> : <FolderOutlined />}
            <FormattedMessage id='collectionListing.header.title' />
          </CollectionListingLink>
        </Menu.Item>

        <Menu.Item key='about'>
          <AboutLink>
            <InfoCircleOutlined />
            <FormattedMessage id='about.title' />
          </AboutLink>
        </Menu.Item>

        {/*<Menu.Item key='images'>
        <ImageListingLink>
          <PictureOutlined />
          <span>Images</span>
        </ImageListingLink>
      </Menu.Item>

      <Menu.Item key='wiki'>
        <WikiHomeLink>
          <WikiOutlined />
          <span>Wiki</span>
        </WikiHomeLink>
      </Menu.Item>

      <Menu.Item key='uploads'>
        <UploadListingLink>
          <CloudUploadOutlined />
          <span>Uploads</span>
        </UploadListingLink>
      </Menu.Item>

      <Menu.Item key='users'>
        <UserSelfInfoLink>
          <UserOutlined />
          <span>Profile</span>
        </UserSelfInfoLink>
      </Menu.Item>

      {user && isElevatedUser(user) &&
        <Menu.Item key='_internal'>
          <AdminPanelLink>
            <ControlOutlined />
            <span>Administration</span>
          </AdminPanelLink>
        </Menu.Item>} */}
      </Menu>

      <a target='_blank' rel="noopener noreferrer" href={`https://github.com/chiyadev/nhitomi/commit/${client.currentInfo.version.hash}`}>
        <Button type='text' style={{
          position: 'absolute',
          left: 0,
          bottom: 0,
          minWidth: SideBarWidth,
          textAlign: 'center',
          display: sidebar ? undefined : 'none'
        }}>
          <small>ver. <code>{client.currentInfo.version.shortHash}</code></small>
        </Button>
      </a>
    </div>
  </Layout.Sider>
}
