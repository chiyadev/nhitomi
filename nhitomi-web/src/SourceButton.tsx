import React, { CSSProperties, useContext, ComponentProps } from 'react'
import { ScraperType } from './Client'
import { ClientContext } from './ClientContext'
import { Button } from 'antd'

export const SourceIcon = ({ type, style }: {
  type: ScraperType
  style?: CSSProperties
}) => {
  return (
    <img
      alt={type}
      src={`/assets/icons/${type}.jpg`}
      style={{
        ...style,
        borderRadius: 2
      }} />
  )
}

const padding = 2

export const SourceButton = ({ type, ...props }: Omit<ComponentProps<typeof Button>, 'type'> & {
  type: ScraperType
}) => {
  const client = useContext(ClientContext)
  const scraper = client.currentInfo.scrapers.find(s => s.type === type)

  return (
    <Button
      {...props}
      style={{
        ...props.style,
        padding: padding,
        paddingRight: `calc(${padding}px + 0.5em)`
      }}
      icon={(
        <SourceIcon
          type={type}
          style={{
            width: 'auto',
            height: '100%',
            marginRight: '0.5em'
          }} />
      )}>

      <span>{scraper?.name || type}</span>
    </Button>
  )
}
