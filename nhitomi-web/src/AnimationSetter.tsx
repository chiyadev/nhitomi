import { useConfig } from './ConfigManager'
import { useLayoutEffect, useRef } from 'react'
import { Globals } from 'react-spring'
import { useProgress } from './ProgressManager'

export const AnimationSetter = () => {
  const [mode] = useConfig('animation')

  const offset = useRef(0)
  const changed = useRef(0)

  useLayoutEffect(() => {
    offset.current = Globals.now()
    changed.current = Date.now()

    const speed = mode === 'faster' ? 4 : 1

    Globals.assign({
      skipAnimation: mode === 'none',
      now: () => {
        const now = Date.now()
        return offset.current + (now - changed.current) * speed
      }
    })
  }, [mode])

  // progress maintains its own animation mode, so sync it
  const { setMode: setProgressMode } = useProgress()

  useLayoutEffect(() => {
    setProgressMode(mode)
  }, [mode, setProgressMode])

  return null
}
