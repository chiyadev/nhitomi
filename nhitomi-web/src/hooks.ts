import { useTitle } from 'react-use'

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
