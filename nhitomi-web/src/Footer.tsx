import React, { useState, ReactNode, useMemo } from 'react'
import { Container } from './Components/Container'
import { NewTabLink } from './Components/NewTabLink'
import { useSpring, animated } from 'react-spring'
import { getColor } from './theme'
import { useClientInfo } from './ClientManager'
import { Tooltip } from './Components/Tooltip'
import { FormattedDate, FormattedTime } from 'react-intl'
import { GitCommit } from 'nhitomi-api'

export const Footer = () => {
  const { info } = useClientInfo()

  const style = useSpring({
    from: { marginBottom: -5, opacity: 0 },
    to: { marginBottom: 0, opacity: 1 }
  })

  return (
    <Container className='text-xs text-gray-darker p-4 text-center space-y-1 overflow-hidden'>
      <animated.div style={style}>
        {useMemo(() => <>
          <VersionTooltip version={info.version}>
            <NewTabLink href={`https://github.com/chiyadev/nhitomi/commit/${info.version.hash}`}>
              <LinkText>b.{info.version.shortHash}</LinkText>
            </NewTabLink>
          </VersionTooltip>
          <Split />
          <NewTabLink href='https://github.com/chiyadev/nhitomi'>
            <LinkText>GitHub</LinkText>
          </NewTabLink>
          <Split />
          <NewTabLink href='https://discord.gg/JFNga7q'>
            <LinkText>Discord</LinkText>
          </NewTabLink>
          <Split />
          <NewTabLink href='https://github.com/chiyadev/nhitomi/wiki/API'>
            <LinkText>API</LinkText>
          </NewTabLink>
          <Split />
          <NewTabLink href='https://chiya.dev'>
            <LinkText>chiya.dev</LinkText>
          </NewTabLink>
        </>, [info.version])}
      </animated.div>
    </Container>
  )
}

const Split = () => <span className='mx-2'>·</span>

const VersionTooltip = ({ version, children }: { version: GitCommit, children?: ReactNode }) => (
  <Tooltip
    className='inline-flex'
    overlayClassName='text-center'
    placement='top'
    overlay={<>
      <div>{version.hash}</div>
      <div><FormattedDate value={version.time} /> <FormattedTime value={version.time} /></div>
    </>}
    children={children} />
)

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
