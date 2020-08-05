import React from 'react'
import { Sidebar } from './Sidebar'
import { LayoutManager } from './LayoutManager'
import { ProgressManager } from './ProgressManager'
import { PrefetchScrollPreserver } from './Prefetch'
import { ClientManager } from './ClientManager'
import { LocaleManager } from './LocaleManager'
import { NotificationManager } from './NotificationManager'

export const App = () => {
  return (
    <LayoutManager>
      <ProgressManager>
        <ClientManager>
          <LocaleManager>
            <NotificationManager>
              <PrefetchScrollPreserver />

              <Sidebar />
            </NotificationManager>
          </LocaleManager>
        </ClientManager>
      </ProgressManager>
    </LayoutManager>
  )
}
