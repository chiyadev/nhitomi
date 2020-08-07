import React from 'react'
import { Sidebar } from './Sidebar'
import { LayoutManager, useLayout } from './LayoutManager'
import { ProgressManager } from './ProgressManager'
import { PrefetchScrollPreserver } from './Prefetch'
import { ClientManager } from './ClientManager'
import { LocaleManager } from './LocaleManager'
import { NotificationManager } from './NotificationManager'
import { cx, css } from 'emotion'
import { Route, Redirect } from 'wouter-preact'
import { BookListingPage } from './BookListing'

export const App = () => {
  return (
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
      <div className={cx('mx-auto w-full', { [css`max-width: ${width}px;`]: !!width })}>
        <Routing />
      </div>
    </div>
  )
}

const Routing = () => <>
  <Route path='/'><Redirect to='/books' /></Route>

  <Route path='/books'><BookListingPage /></Route>
</>
