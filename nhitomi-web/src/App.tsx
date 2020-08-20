import React, { useMemo } from 'react'
import { Sidebar } from './Sidebar'
import { LayoutManager } from './LayoutManager'
import { ProgressManager } from './ProgressManager'
import { PrefetchScrollPreserver } from './Prefetch'
import { ClientManager } from './ClientManager'
import { LocaleManager } from './LocaleManager'
import { NotificationManager } from './NotificationManager'
import { Route, Redirect, Switch, Router } from 'react-router-dom'
import { BookListing } from './BookListing'
import { Settings } from './Settings'
import { Navigator, useNavigator } from './state'
import { AnimationSetter } from './AnimationSetter'
import { ConfigManager } from './ConfigManager'
import { BookReader } from './BookReader'
import { BlurSetter } from './BlurSetter'
import { CollectionListing } from './CollectionListing'
import { CollectionContent } from './CollectionContent'
import { TitleSetter } from './TitleSetter'
import { Debug } from './Debug'
import { Authentication } from './Authentication'
import { OAuthCallback } from './Authentication/OAuthCallback'
import { CollectionEdit } from './CollectionListing/Edit'
import { Footer } from './Footer'
import { About } from './About'

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
                  <TitleSetter />

                  <Sidebar>
                    <Body />
                  </Sidebar>
                </NotificationManager>
              </LocaleManager>
            </ClientManager>
          </ProgressManager>
        </LayoutManager>
      </ConfigManager>
    </Router>
  )
}

const Body = () => (
  <div className='flex flex-col min-h-screen'>
    <div className='relative w-full flex-1'>
      <Routing />
    </div>
    <div className='mt-4'>
      <Footer />
    </div>
  </div>
)

const Routing = () => {
  const { path } = useNavigator()

  return useMemo(() => (
    <Switch location={{ pathname: path, search: '', hash: '', state: undefined }}>
      <Route path='/' exact><Redirect to='/books' /></Route>
      <Route path='/about' exact component={About} />

      <Route path='/auth' exact component={Authentication} />
      <Route path='/oauth/:service' exact render={({ match: { params: { service } } }) => <OAuthCallback service={service} />} />

      <Route path='/books' exact component={BookListing} />
      <Route path='/books/:id/contents/:contentId' exact render={({ match: { params: { id, contentId } } }) => <BookReader id={id} contentId={contentId} />} />

      <Route path='/collections/:id' exact render={({ match: { params: { id } } }) => <CollectionContent id={id} />} />
      <Route path='/collections/:id/edit' exact render={({ match: { params: { id } } }) => <CollectionEdit id={id} />} />

      <Route path='/users/:id/collections' exact render={({ match: { params: { id } } }) => <CollectionListing id={id} />} />

      <Route path='/settings' exact component={Settings} />
      <Route path='/settings/debug' exact component={Debug} />
    </Switch>
  ), [path])
}
