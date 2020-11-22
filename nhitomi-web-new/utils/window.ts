import { useEffect, useState } from "react";

export function useWindowSize() {
  const [size, setSize] = useState<[number, number]>();

  useEffect(() => {
    const handler = () => {
      setSize([window.innerWidth, window.innerHeight]);
    };

    handler();

    window.addEventListener("resize", handler);
    return () => window.removeEventListener("resize", handler);
  }, []);

  return size;
}
