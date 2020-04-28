import './App.css'

import { Layout, message as antd_alert, notification as antd_notif } from 'antd'
import React, { useContext } from 'react'
import { BrowserRouter, Switch } from 'react-router-dom'
import { NotificationProvider } from './NotificationContext'
import { LayoutProvider, LayoutContext } from './LayoutContext'
import { ProgressBarProvider } from './Progress'
import { ClientProvider } from './ClientContext'
import { SideBar, SideBarWidth } from './Sidebar'
import { AuthenticationManager } from './Authentication'
import { LocaleProvider } from './LocaleProvider'

export const App = () => {
  // https://ant.design/components/notification/#FAQ
  const [notif, notifNode] = antd_notif.useNotification()

  // note: order matters!!
  return <React.StrictMode>
    <BrowserRouter>
      <NotificationProvider notif={notif} alert={antd_alert}>
        <ProgressBarProvider>
          <LocaleProvider>
            <ClientProvider>
              <LayoutProvider>
                {notifNode}

                <Layout style={{ minHeight: '100vh' }}>
                  <SideBar />

                  <AuthenticationManager>
                    <Routing />
                  </AuthenticationManager>
                </Layout>
              </LayoutProvider>
            </ClientProvider>
          </LocaleProvider>
        </ProgressBarProvider>
      </NotificationProvider>
    </BrowserRouter>
  </React.StrictMode>
}

const Routing = () => {
  const { sidebar, mobile } = useContext(LayoutContext)

  return <Layout style={{ marginLeft: sidebar && !mobile ? SideBarWidth : 0 }}>
    <Switch>
    </Switch>
  </Layout>
}
