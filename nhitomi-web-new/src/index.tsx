import React from 'react'
import { render } from 'react-dom'
import { App } from './App'

import './theme.css'
import './index.css'

render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
  document.getElementById('root')
)
