import React, { ReactNode } from 'react'
import { useSpring, animated } from 'react-spring'

export const PageContainer = ({ children, className }: { children?: ReactNode, className?: string }) => {
  const style = useSpring({
    from: { opacity: 0 },
    to: { opacity: 1 }
  })

  return <animated.div style={style} className={className} children={children} />
}
