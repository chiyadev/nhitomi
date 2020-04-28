const { override, fixBabelImports, addLessLoader, addWebpackAlias } = require('customize-cra')
const symlinkDir = require('symlink-dir')

// symlink locale folder into src for webpack import
symlinkDir('../locales', './src/locales')

const theme = require('@ant-design/dark-theme')
const colors = require('./src/colors.json')

module.exports = override(
  // replace react with preact
  addWebpackAlias({
    'react': 'preact/compat',
    'react-dom': 'preact/compat'
  }),

  // required for antd
  fixBabelImports('import', {
    libraryName: 'antd',
    libraryDirectory: 'es',
    style: true
  }),

  // theme configuration
  addLessLoader({
    javascriptEnabled: true,
    modifyVars: {
      ...theme.default,
      ...colors
    }
  })
)
