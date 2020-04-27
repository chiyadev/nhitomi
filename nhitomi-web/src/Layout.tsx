import React, { ComponentProps, useContext } from 'react'
import { Layout } from 'antd'
import { LayoutContext } from './LayoutContext'

/** Shorthand for Layout.Content with some styling. */
export const LayoutContent = ({ style, ...props }: ComponentProps<typeof Layout.Content>) => {
  const { mobile } = useContext(LayoutContext)

  return <Layout.Content
    style={{
      marginLeft: mobile ? undefined : '1em',
      marginRight: mobile ? undefined : '1em',
      padding: '1em',
      ...style
    }}
    {...props} />
}
