import { useEffect, useMemo, useRef, useState } from "react";

export function useTimerOnce(ms: number) {
  const [value, setValue] = useState(false);

  useEffect(() => {
    window.setTimeout(() => setValue(true), ms);
  }, []);

  return value;
}

export function useChangeCount(value: any) {
  const count = useRef(0);
  useMemo(() => count.current++, [value]);

  return count.current;
}
