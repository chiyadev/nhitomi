const { override, fixBabelImports, addLessLoader, addWebpackAlias } = require('customize-cra')

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
    lessOptions: {
      javascriptEnabled: true,
      modifyVars: {
        ...theme.default,
        ...colors
      }
    }
  })
)
