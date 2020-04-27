const { override, fixBabelImports, addLessLoader, addWebpackAlias } = require('customize-cra')
const theme = require('@ant-design/dark-theme')

const colors = require('./src/colors.json')

module.exports = override(
  addWebpackAlias({
    'react': 'preact/compat',
    'react-dom': 'preact/compat'
  }),

  fixBabelImports('import', {
    libraryName: 'antd',
    libraryDirectory: 'es',
    style: true
  }),

  addLessLoader({
    javascriptEnabled: true,
    modifyVars: {
      ...theme.default,
      ...colors
    }
  })
)
