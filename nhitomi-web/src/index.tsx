import React from 'react'
import ReactDOM from 'react-dom'
import { register as registerServiceWorker } from './serviceWorker'
import { App } from './App'

ReactDOM.render(<App />, document.getElementById('root'))

registerServiceWorker()
