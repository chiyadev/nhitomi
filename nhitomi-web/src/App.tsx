import React, { useMemo } from 'react'
import { Sidebar } from './Sidebar'
import { LayoutManager, useLayout } from './LayoutManager'
import { ProgressManager } from './ProgressManager'
import { PrefetchScrollPreserver } from './Prefetch'
import { ClientManager } from './ClientManager'
import { LocaleManager } from './LocaleManager'
import { NotificationManager } from './NotificationManager'
import { css } from 'emotion'
import { Route, Redirect, Switch, Router } from 'react-router-dom'
import { BookListing } from './BookListing'
import { Settings } from './Settings'
import { Navigator, useNavigator } from './state'
import { AnimationSetter } from './AnimationSetter'
import { ConfigManager } from './ConfigManager'
import { BookReader } from './BookReader'
import { BlurSetter } from './BlurSetter'

export const App = () => {
  return (
    <Router history={Navigator.history}>
      <ConfigManager>
        <LayoutManager>
          <ProgressManager>
            <ClientManager>
              <LocaleManager>
                <NotificationManager>
                  <PrefetchScrollPreserver />
                  <AnimationSetter />
                  <BlurSetter />

                  <Sidebar />
                  <Body />
                </NotificationManager>
              </LocaleManager>
            </ClientManager>
          </ProgressManager>
        </LayoutManager>
      </ConfigManager>
    </Router>
  )
}

const Body = () => {
  const { sidebar } = useLayout()

  return (
    <div className={css`padding-left: ${sidebar}px;`}>
      <div className='relative w-full'>
        <Routing />
      </div>
    </div>
  )
}

const Routing = () => {
  const { path } = useNavigator()

  return useMemo(() => (
    <Switch location={{ pathname: path, search: '', hash: '', state: undefined }}>
      <Route path='/' exact><Redirect to='/books' /></Route>

      <Route path='/books' exact component={BookListing} />
      <Route path='/books/:id/contents/:contentId' exact render={({ match: { params: { id, contentId } } }) => <BookReader id={id} contentId={contentId} />} />

      <Route path='/settings' exact component={Settings} />
    </Switch>
  ), [path])
}
