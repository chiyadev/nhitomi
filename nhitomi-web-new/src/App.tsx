import React from 'react'
import { Sidebar } from './Sidebar'
import { LayoutManager, useLayout } from './LayoutManager'
import { ProgressManager } from './ProgressManager'
import { PrefetchScrollPreserver } from './Prefetch'
import { ClientManager } from './ClientManager'
import { LocaleManager } from './LocaleManager'
import { NotificationManager } from './NotificationManager'
import { css } from 'emotion'
import { Route, Redirect, Switch, Router } from 'react-router-dom'
import { BookListingPage } from './BookListing'
import { History } from './history'

export const App = () => {
  return (
    <Router history={History}>
      <LayoutManager>
        <ProgressManager>
          <ClientManager>
            <LocaleManager>
              <NotificationManager>
                <PrefetchScrollPreserver />

                <Sidebar />
                <Body />
              </NotificationManager>
            </LocaleManager>
          </ClientManager>
        </ProgressManager>
      </LayoutManager>
    </Router>
  )
}

const Body = () => {
  const { sidebar } = useLayout()

  return (
    <div className={css`padding-left: ${sidebar}px;`}>
      <Routing />
    </div>
  )
}

const Routing = () => (
  <Switch>
    <Route path='/' exact><Redirect to='/books' /></Route>

    <Route path='/books' exact><BookListingPage /></Route>
  </Switch>
)
