import React from 'react'
import { Sidebar } from './Sidebar'
import { LayoutManager } from './LayoutManager'
import { ProgressManager } from './ProgressManager'
import { PrefetchScrollPreserver } from './Prefetch'
import { ClientManager } from './ClientManager'

export const App = () => {
  return <>
    <LayoutManager>
      <ProgressManager>
        <ClientManager>
          <PrefetchScrollPreserver />
          <Sidebar />
        </ClientManager>
      </ProgressManager>
    </LayoutManager>
  </>
}
