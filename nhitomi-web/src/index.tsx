import React from 'react'
import ReactDOM from 'react-dom'
import { register as registerServiceWorker } from './serviceWorker'
import { App } from './App'

import './localStorage'
import './logging'
import './colors'

registerServiceWorker()

ReactDOM.render(<App />, document.getElementById('root'))
