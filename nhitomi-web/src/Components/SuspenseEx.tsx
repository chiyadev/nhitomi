import React, { Suspense, useLayoutEffect } from 'react'
import { ReactNode } from 'react'
import { useProgress } from '../ProgressManager'

/** Similar to React.Suspense but with built-in fallback handling. */
export const SuspenseEx = ({ children }: { children?: ReactNode }) => (
  <Suspense fallback={<Fallback />} children={children} />
)

const Fallback = () => {
  const { begin, end } = useProgress()

  useLayoutEffect(() => {
    begin()
    return end
  }, [begin, end])

  return null
}
