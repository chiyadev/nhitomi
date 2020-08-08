import React from 'react'
import { render } from 'react-dom'
import { App } from './App'
import Modal from 'react-modal'

import './theme.css'
import './theme-special'
import './index.css'

Modal.setAppElement('#root')

render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
  document.getElementById('root')
)
