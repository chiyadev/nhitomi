import React from 'react'
import { Sidebar } from './Sidebar'
import { LayoutManager, useLayout } from './LayoutManager'
import { ProgressManager } from './ProgressManager'
import { PrefetchScrollPreserver } from './Prefetch'
import { ClientManager } from './ClientManager'
import { LocaleManager } from './LocaleManager'
import { NotificationManager } from './NotificationManager'
import { cx, css } from 'emotion'
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
  const { width: windowWidth, sidebar } = useLayout()
  let width: number | undefined

  for (const breakpoint of [640, 768, 1024, 1280]) {
    if (windowWidth >= breakpoint - sidebar)
      width = breakpoint - sidebar

    else break
  }

  return (
    <div className={css`padding-left: ${sidebar}px;`}>
      <div className={cx('relative mx-auto w-full', { [css`max-width: ${width}px;`]: !!width })}>
        <Routing />
      </div>
    </div>
  )
}

const Routing = () => (
  <Switch>
    <Route path='/' exact><Redirect to='/books' /></Route>

    <Route path='/books' exact><BookListingPage /></Route>
  </Switch>
)
