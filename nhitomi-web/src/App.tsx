import React, { useMemo, useLayoutEffect } from 'react'
import { Route, Redirect, Switch, Router } from 'react-router-dom'
import { Navigator, useNavigator } from './state'
import { ProgressManager } from './ProgressManager'
import { pageview } from 'react-ga'

import { ConfigManager } from './ConfigManager'
import { LayoutManager } from './LayoutManager'
import { ClientManager } from './ClientManager'
import { LocaleManager } from './LocaleManager'
import { NotificationManager } from './NotificationManager'
import { PrefetchScrollPreserver } from './Prefetch'
import { AnimationSetter } from './AnimationSetter'
import { BlurSetter } from './BlurSetter'
import { TitleSetter } from './TitleSetter'
import { Sidebar } from './Sidebar'
import { MaintenanceHeader } from './MaintenanceHeader'
import { Footer } from './Footer'
import { ShortcutHintDisplay } from './ShortcutHintDisplay'

import { About } from './About'
import { Support } from './Support'
import { Pending as SupportPending } from './Support/Pending'
import { Authentication } from './Authentication'
import { OAuthCallback } from './Authentication/OAuthCallback'
import { BookListing } from './BookListing'
import { BookReader } from './BookReader'
import { CollectionContent } from './CollectionContent'
import { CollectionEdit } from './CollectionListing/Edit'
import { CollectionListing } from './CollectionListing'
import { Settings } from './Settings'
import { Debug } from './Internal/Debug'
import { NotFound } from './NotFound'

export const App = () => {
  return (
    <Router history={Navigator.history}>
      <ProgressManager>
        <ConfigManager>
          <LayoutManager>
            <ClientManager>
              <LocaleManager>
                <NotificationManager>
                  <PrefetchScrollPreserver />
                  <AnimationSetter />
                  <BlurSetter />
                  <TitleSetter />
                  <ShortcutHintDisplay />

                  <Sidebar>
                    <div className='flex flex-col min-h-screen'>
                      <div className='relative w-full flex-1'>
                        <MaintenanceHeader />

                        <Routing />
                      </div>
                      <div className='mt-4'>
                        <Footer />
                      </div>
                    </div>
                  </Sidebar>
                </NotificationManager>
              </LocaleManager>
            </ClientManager>
          </LayoutManager>
        </ConfigManager>
      </ProgressManager>
    </Router>
  )
}

const Routing = () => {
  const { path, query, stringify, evaluate } = useNavigator()
  const gapath = useMemo(() => stringify(evaluate({ path, query })), [evaluate, path, query, stringify])

  useLayoutEffect(() => { pageview(gapath) }, [gapath])

  return useMemo(() => (
    <Switch location={{ pathname: path, search: '', hash: '', state: undefined }}>
      <Route path='/' exact><Redirect to='/books' /></Route>
      <Route path='/about' exact component={About} />

      <Route path='/support' exact component={Support} />
      <Route path='/support/pending' exact component={SupportPending} />

      <Route path='/auth' exact component={Authentication} />
      <Route path='/oauth/:service' exact render={({ match: { params: { service } } }) => <OAuthCallback service={service} />} />

      <Route path='/books' exact component={BookListing} />
      <Route path='/books/:id/contents/:contentId' exact render={({ match: { params: { id, contentId } } }) => <BookReader id={id} contentId={contentId} />} />

      <Route path='/collections/:id' exact render={({ match: { params: { id } } }) => <CollectionContent id={id} />} />
      <Route path='/collections/:id/edit' exact render={({ match: { params: { id } } }) => <CollectionEdit id={id} />} />

      <Route path='/users/:id/collections' exact render={({ match: { params: { id } } }) => <CollectionListing id={id} />} />

      <Route path='/settings' exact component={Settings} />
      <Route path='/settings/debug' exact component={Debug} />

      <Route component={NotFound} />
    </Switch>
  ), [path])
}
