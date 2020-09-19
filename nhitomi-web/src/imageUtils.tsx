// @ts-ignore
import { sync as probe } from "probe-image-size";

export async function probeImage(data: Blob): Promise<{
  width: number
  height: number
  type: string
  mime: string
  wUnits: string
  hUnits: string
}> {
  return probe(new Uint8Array(await new Response(data).arrayBuffer()));
}
