import { Divider, Layout, Menu, PageHeader } from 'antd'
import React, { useContext, useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { useShortcut } from './shortcuts'
import { NotificationContext } from './NotificationContext'
import { ClientContext } from './ClientContext'
import { LayoutContext } from './LayoutContext'

export const SideBarWidth = 200

export const SideBar = () => {
  const { pathname } = useLocation()
  const client = useContext(ClientContext)
  const { alert } = useContext(NotificationContext)
  const { sidebar, setSidebar, mobile } = useContext(LayoutContext)

  // collapse on url change if mobile
  // we use setTimeout to avoid a bug in antd where menu shows a tooltip that can't be disabled
  useEffect(() => { mobile && sidebar && setTimeout(() => setSidebar(false), 100) }, [pathname]) // eslint-disable-line

  // collapse on shortcut
  useShortcut('sidebarKey', () => {
    setSidebar(!sidebar)

    if (sidebar)
      alert.info('Sidebar collapsed.')
    else
      alert.info('Sidebar opened.')
  })

  return <Layout.Sider
    breakpoint='md'
    onBreakpoint={v => setSidebar(!v)}
    width={SideBarWidth}
    collapsible
    collapsed={!sidebar}
    collapsedWidth={0}
    onCollapse={v => setSidebar(!v)}
    style={{
      position: 'fixed',
      height: '100vh',
      zIndex: 100,
      top: 0,
      left: 0,
      boxShadow: sidebar ? '-3px 0 6px 0 #555' : undefined
    }}>
    <div style={{
      opacity: sidebar ? 1 : 0,
      transition: sidebar ? 'opacity 0.5s' : undefined
    }}>
      {/* <HomeLink> */}
      <PageHeader
        backIcon={false}
        style={{ minWidth: SideBarWidth }}
        title='nhitomi' />
      {/* </HomeLink> */}

      <Divider style={{ margin: 0 }} />

      <Menu
        mode='inline'
        style={{
          height: '100%' // needed to make vertical divider
        }}
        selectedKeys={[pathname.split('/')[1]]}>

        {/* <Menu.Item key='books'>
        <BookListingLink>
          <BookOutlined />
          <span>Books</span>
        </BookListingLink>
      </Menu.Item>

      <Menu.Item key='images'>
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

      <div style={{
        position: 'absolute',
        left: 0,
        bottom: 0,
        minWidth: SideBarWidth,
        textAlign: 'center'
      }}>
        <label><small>ver. <code>{client.currentInfo.version.shortHash}</code></small></label>
      </div>
    </div>
  </Layout.Sider>
}
