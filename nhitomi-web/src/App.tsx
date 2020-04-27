import './App.css'

import { Layout, message as antd_alert, notification as antd_notif } from 'antd'
import React, { useContext } from 'react'
import { IntlProvider } from 'react-intl'
import { BrowserRouter, Switch } from 'react-router-dom'
import { NotificationProvider } from './NotificationContext'
import { LayoutProvider, LayoutContext } from './LayoutContext'
import { ProgressBarProvider } from './Progress'
import { ClientProvider } from './ClientContext'
import { SideBar, SideBarWidth } from './Sidebar'

export const App = () => {
  // https://ant.design/components/notification/#FAQ
  const [notif, notifNode] = antd_notif.useNotification()

  // note: provider order matters!!
  return <React.StrictMode>
    <IntlProvider locale='en'>
      <BrowserRouter>
        <NotificationProvider notif={notif} alert={antd_alert}>
          <LayoutProvider>
            <ProgressBarProvider>
              <ClientProvider>
                {notifNode}

                <Layout style={{ minHeight: '100vh' }}>
                  <SideBar />
                  <Routing />
                </Layout>
              </ClientProvider>
            </ProgressBarProvider>
          </LayoutProvider>
        </NotificationProvider>
      </BrowserRouter>
    </IntlProvider>
  </React.StrictMode>
}

const Routing = () => {
  const { sidebar } = useContext(LayoutContext)

  return <Layout style={{ marginLeft: sidebar ? SideBarWidth : 0 }}>
    <Switch>
    </Switch>
  </Layout>
}
