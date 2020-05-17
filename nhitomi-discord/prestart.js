/* eslint-disable @typescript-eslint/no-var-requires */
const symlink = require('symlink-dir')

symlink('../locales', 'build/locales')
symlink('config', 'build/config')
