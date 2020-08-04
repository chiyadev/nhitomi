import React, { useContext } from 'react'
import { LayoutContext } from '../LayoutManager'
import { Overlay } from './Overlay'
import { Strip } from './Strip'

export const Sidebar = () => {
  const { screen } = useContext(LayoutContext)

  switch (screen) {
    case 'sm': return <Overlay />
    case 'lg': return <Strip />
  }
}
