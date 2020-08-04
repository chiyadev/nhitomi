import React from 'react'
import { Sidebar } from './Sidebar'
import { LayoutManager } from './LayoutManager'

export const App = () => {
  return <>
    <LayoutManager>
      <Sidebar />
    </LayoutManager>
  </>
}
