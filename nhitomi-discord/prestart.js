/* eslint-disable @typescript-eslint/no-var-requires */
const symlink = require('symlink-dir')

symlink('../locales', 'build/locales')
symlink('src/config', 'build/config')
