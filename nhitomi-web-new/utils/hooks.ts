import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useHotkeys, useIsHotkeyPressed } from "react-hotkeys-hook";
import { useToast } from "@chakra-ui/react";
import { useT } from "../locales";

export function useTimerOnce(ms: number) {
  const [value, setValue] = useState(false);

  useEffect(() => {
    window.setTimeout(() => setValue(true), ms);
  }, []);

  return value;
}

export function useChangeCount(value: any) {
  const count = useRef(0);
  return useMemo(() => count.current++, [value]);
}

export function useLastValue<T>(value: T) {
  const ref = useRef(value);
  const last = ref.current;

  ref.current = value;
  return last;
}

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

export function useWindowSize() {
  const getCurrent = useCallback((): [number, number] => [document.body.clientWidth, window.innerHeight], []);
  const [state, setState] = useState(getCurrent);

  useEffect(() => {
    const handler = () => setState(getCurrent());
    handler();

    window.addEventListener("resize", handler);
    return () => window.removeEventListener("resize", handler);
  }, [getCurrent]);

  return state;
}

export function useWindowScroll() {
  const getCurrent = useCallback((): [number, number] => [window.scrollX, window.scrollY], []);
  const [state, setState] = useState(getCurrent);

  useEffect(() => {
    const handler = () => setState(getCurrent());
    handler();

    window.addEventListener("scroll", handler);
    return () => window.removeEventListener("scroll", handler);
  }, [getCurrent]);

  return state;
}

// stateful useIsHotkeyPressed
export function useHotkeyState(keys: string) {
  const pressed = useIsHotkeyPressed();
  const [state, setState] = useState(false);

  useHotkeys(
    keys,
    (e) => {
      e.preventDefault();

      setTimeout(() => {
        let state = false;

        for (const k of keys.split(",")) {
          state = state || pressed(k.trim());
        }

        setState(state);
      });
    },
    {
      keydown: true,
      keyup: true,
    },
    [pressed]
  );

  return state;
}

export function useErrorToast() {
  const t = useT();
  const toast = useToast();

  return (error: Error) => {
    return toast({
      title: t("error"),
      description: error.message,
      position: "top-right",
      status: "error",
      isClosable: true,
    });
  };
}
