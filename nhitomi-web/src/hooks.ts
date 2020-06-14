import { useTitle, useUpdate } from 'react-use'
import StrictEventEmitter from 'strict-event-emitter-types'
import { EventEmitter } from 'events'
import { useLayoutEffect } from 'react'

/**
 * Sets the name of the current tab.
 * Node that the title will not be restored automatically.
 * @param titleParts parts of the title string that will be concatenated
 */
export function useTabTitle(...titleParts: (string | undefined)[]) {
  const title = titleParts.map(p => p?.trim()).filter(p => p).concat(['nhitomi']).join(' · ')

  return useTitle(title, {
    restoreOnUnmount: false
  })
}

/**
 * Rerenders a component when the given emitter emits an event of a specific name.
 * @param emitter emitter to listen to the events of
 * @param event event name
 */
export function useUpdateOnEvent<TEmitter extends StrictEventEmitter<EventEmitter, TEventRecord>, TEventRecord extends {}>(emitter: TEmitter, event: keyof TEventRecord) {
  const rerender = useUpdate()

  useLayoutEffect(() => {
    emitter.on(event, rerender)

    return () => { emitter.off(event as string, rerender) }
  }, [emitter, event, rerender])
}
