import React, { useMemo, useLayoutEffect, lazy, ExoticComponent } from 'react'
import { Route, Redirect, Switch, Router } from 'react-router-dom'
import { Navigator, useNavigator } from './state'
import { ProgressManager } from './ProgressManager'
import { pageview } from 'react-ga'
import { SuspenseEx } from './Components/SuspenseEx'

function lazyEx<T extends { [key: string]: any }>(factory: () => Promise<T>, component: keyof T) {
  return lazy(() => factory().then(module => ({ default: module[component] })))
}

function WrapSuspense<T>(Component: ExoticComponent<T>) {
  return (props: T) => (
    <SuspenseEx>
      <Component {...props} />
    </SuspenseEx>
  )
}

const ConfigManager = WrapSuspense(lazyEx(() => import('./ConfigManager'), 'ConfigManager'))
const LayoutManager = WrapSuspense(lazyEx(() => import('./LayoutManager'), 'LayoutManager'))
const ClientManager = WrapSuspense(lazyEx(() => import('./ClientManager'), 'ClientManager'))
const LocaleManager = WrapSuspense(lazyEx(() => import('./LocaleManager'), 'LocaleManager'))
const NotificationManager = WrapSuspense(lazyEx(() => import('./NotificationManager'), 'NotificationManager'))
const PrefetchScrollPreserver = WrapSuspense(lazyEx(() => import('./Prefetch'), 'PrefetchScrollPreserver'))
const AnimationSetter = WrapSuspense(lazyEx(() => import('./AnimationSetter'), 'AnimationSetter'))
const BlurSetter = WrapSuspense(lazyEx(() => import('./BlurSetter'), 'BlurSetter'))
const TitleSetter = WrapSuspense(lazyEx(() => import('./TitleSetter'), 'TitleSetter'))
const Sidebar = WrapSuspense(lazyEx(() => import('./Sidebar'), 'Sidebar'))
const MaintenanceHeader = WrapSuspense(lazyEx(() => import('./MaintenanceHeader'), 'MaintenanceHeader'))
const Footer = WrapSuspense(lazyEx(() => import('./Footer'), 'Footer'))

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

const About = WrapSuspense(lazyEx(() => import('./About'), 'About'))
const Support = WrapSuspense(lazyEx(() => import('./Support'), 'Support'))
const SupportPending = WrapSuspense(lazyEx(() => import('./Support/Pending'), 'Pending'))
const Authentication = WrapSuspense(lazyEx(() => import('./Authentication'), 'Authentication'))
const OAuthCallback = WrapSuspense(lazyEx(() => import('./Authentication/OAuthCallback'), 'OAuthCallback'))
const BookListing = WrapSuspense(lazyEx(() => import('./BookListing'), 'BookListing'))
const BookReader = WrapSuspense(lazyEx(() => import('./BookReader'), 'BookReader'))
const CollectionContent = WrapSuspense(lazyEx(() => import('./CollectionContent'), 'CollectionContent'))
const CollectionEdit = WrapSuspense(lazyEx(() => import('./CollectionListing/Edit'), 'CollectionEdit'))
const CollectionListing = WrapSuspense(lazyEx(() => import('./CollectionListing'), 'CollectionListing'))
const Settings = WrapSuspense(lazyEx(() => import('./Settings'), 'Settings'))
const Debug = WrapSuspense(lazyEx(() => import('./Internal/Debug'), 'Debug'))
const NotFound = WrapSuspense(lazyEx(() => import('./NotFound'), 'NotFound'))

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
