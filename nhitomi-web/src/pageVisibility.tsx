/** Returns a promise that resolves when the document becomes visible. */
export async function waitDocumentVisible() {
  while (document.hidden) await new Promise((r) => setTimeout(r, 100));
}
