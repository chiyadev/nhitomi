import React, { ComponentProps } from 'react'

export const NewTabLink = (props: ComponentProps<'a'>) => {
  return (
    // eslint-disable-next-line
    <a target='_blank' rel='noopener noreferrer' {...props} />
  )
}
