import { useEffect, useState } from "react";

export function useTimerOnce(ms: number) {
  const [value, setValue] = useState(false);

  useEffect(() => {
    window.setTimeout(() => setValue(true), ms);
  }, []);

  return value;
}
