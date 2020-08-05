import React from 'react'
import { useLayout } from '../LayoutManager'
import { Overlay } from './Overlay'
import { Strip } from './Strip'

export const Sidebar = () => {
  const { screen } = useLayout()

  switch (screen) {
    case 'sm': return <Overlay />
    case 'lg': return <Strip />
  }
}
