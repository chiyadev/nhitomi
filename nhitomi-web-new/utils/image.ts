// @ts-ignore
import sync from "probe-image-size/sync";
import { ProbeResult } from "probe-image-size";

export function probeImage(buffer: ArrayBuffer): ProbeResult {
  return sync(new Uint8Array(buffer));
}
