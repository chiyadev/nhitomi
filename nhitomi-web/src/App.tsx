import { Layout, message as antd_alert, notification as antd_notif } from 'antd'
import React, { useContext } from 'react'
import { BrowserRouter, Switch, Route, Redirect } from 'react-router-dom'
import { NotificationProvider } from './NotificationContext'
import { LayoutProvider, LayoutContext } from './LayoutContext'
import { ProgressBarProvider } from './Progress'
import { ClientProvider } from './ClientContext'
import { SideBar, SideBarWidth } from './Sidebar'
import { AuthenticationManager, AuthenticationRoute } from './Authentication'
import { LocaleProvider } from './LocaleProvider'
import { BookListing } from './BookListing'
import { BookReader } from './BookReader'
import { ExperimentalBanner } from './ExperimentalBanner'

import './App.css'

export const App = () => {
  // https://ant.design/components/notification/#FAQ
  const [notif, notifNode] = antd_notif.useNotification()

  // note: order matters!!!!!
  return <React.StrictMode>
    <BrowserRouter>
      <ProgressBarProvider>
        <LocaleProvider>
          <NotificationProvider notif={notif} alert={antd_alert}>
            <ClientProvider>
              <LayoutProvider>
                {notifNode}

                <Layout style={{ minHeight: '100vh' }}>
                  <SideBar />
                  <Routing />
                </Layout>
              </LayoutProvider>
            </ClientProvider>
          </NotificationProvider>
        </LocaleProvider>
      </ProgressBarProvider>
    </BrowserRouter>
  </React.StrictMode>
}

const Routing = () => {
  const { sidebar, breakpoint } = useContext(LayoutContext)

  return <Layout style={{ marginLeft: sidebar && !breakpoint ? SideBarWidth : 0 }}>
    <ExperimentalBanner />

    <Switch>
      <Route path='/oauth/:service' exact render={({ match: { params } }) => <AuthenticationRoute {...params} />} />

      <Route>
        <AuthenticationManager>
          <Route path='/' exact><Redirect to='/books' /></Route>

          <Route path='/books' exact component={BookListing} />
          <Route path='/books/:id/contents/:contentId' exact render={({ match: { params } }) => <BookReader {...params} />} />
        </AuthenticationManager>
      </Route>
    </Switch>
  </Layout>
}
