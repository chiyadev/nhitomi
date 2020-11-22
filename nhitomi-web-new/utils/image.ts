import { useEffect, useMemo } from "react";

export function useBlobUrl(blob: Blob | undefined): string | undefined {
  const url = useMemo(() => {
    if (blob) {
      return URL.createObjectURL(blob);
    }
  }, [blob]);

  useEffect(
    () => () => {
      if (url) {
        URL.revokeObjectURL(url);
      }
    },
    [url]
  );

  return url;
}
