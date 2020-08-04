import React from 'react'
import { Sidebar } from './Sidebar'
import { LayoutManager } from './LayoutManager'
import { ProgressManager } from './ProgressManager'

export const App = () => {
  return <>
    <LayoutManager>
      <ProgressManager>
        <Sidebar />
      </ProgressManager>
    </LayoutManager>
  </>
}
