import React from 'react'
import { Alert } from 'antd'

export const ExperimentalBanner = () => {
  return <Alert banner message={<span>nhitomi website is a work in progress. Please report any issues or make feature requests on <a href='https://github.com/chiyadev/nhitomi' target='_blank' rel='noopener noreferrer'>GitHub</a>!</span>} />
}
