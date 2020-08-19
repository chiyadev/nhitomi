import React, { useState, ReactNode } from 'react'
import { Container } from './Components/Container'
import { NewTabLink } from './Components/NewTabLink'
import { useSpring, animated } from 'react-spring'
import { getColor } from './theme'
import { useClientInfo } from './ClientManager'
import { Tooltip } from './Components/Tooltip'
import { FormattedDate, FormattedTime } from 'react-intl'

export const Footer = () => {
  const { info } = useClientInfo()

  return (
    <Container className='text-xs text-gray-darker p-4 text-center space-y-1'>
      <div>
        <Tooltip
          className='inline-flex'
          overlayClassName='text-center'
          placement='top'
          overlay={<>
            <div>{info.version.hash}</div>
            <div><FormattedDate value={info.version.time} /> <FormattedTime value={info.version.time} /></div>
          </>}>

          <NewTabLink href={`https://github.com/chiyadev/nhitomi/commit/${info.version.hash}`}>
            <LinkText>b.{info.version.shortHash}</LinkText>
          </NewTabLink>
        </Tooltip>
        <Split />
        <NewTabLink href='https://github.com/chiyadev/nhitomi'>
          <LinkText>GitHub</LinkText>
        </NewTabLink>
        <Split />
        <NewTabLink href='https://discord.gg/JFNga7q'>
          <LinkText>Discord</LinkText>
        </NewTabLink>
        <Split />
        <NewTabLink href='/api/v1'>
          <LinkText>API</LinkText>
        </NewTabLink>
        <Split />
        <NewTabLink href='https://chiya.dev'>
          <LinkText>chiya.dev</LinkText>
        </NewTabLink>
      </div>
    </Container>
  )
}

const Split = () => <span className='mx-2'>·</span>

const LinkText = ({ children }: { children?: ReactNode }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    color: hover ? getColor('white').rgb : getColor('gray', 'darker').rgb
  })

  return (
    <animated.span
      style={style}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      children={children} />
  )
}
