/* eslint-disable @typescript-eslint/no-var-requires */
const symlink = require('symlink-dir')

symlink('config', 'build/config')
symlink('src/Locales', 'build/Locales')
