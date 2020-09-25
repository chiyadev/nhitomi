import { DependencyList, RefObject, useLayoutEffect, useMemo, useState } from "react";
import { useAsyncFn, useLatest } from "react-use";
import { FnReturningPromise } from "react-use/lib/util";
import ResizeObserver from "resize-observer-polyfill";
import { randomStr } from "./random";

// equivalent to react-use's useAsync except callback runs synchronously
export function useAsync<T extends FnReturningPromise>(fn: T, deps: DependencyList = []) {
  const [state, callback] = useAsyncFn(fn, deps, {
    loading: true,
  });

  useLayoutEffect(() => {
    callback();
  }, [callback]);

  return state;
}

export function useWindowSize() {
  const [state, setState] = useState<{
    width: number;
    height: number;
  }>({
    width: window.innerWidth,
    height: window.innerHeight,
  });

  useLayoutEffect(() => {
    const handler = () =>
      setState({
        width: window.innerWidth,
        height: window.innerHeight,
      });

    window.addEventListener("resize", handler);
    return () => {
      window.removeEventListener("resize", handler);
    };
  }, []);

  return state;
}

export function useWindowScroll() {
  const [state, setState] = useState<{
    x: number;
    y: number;
  }>({
    x: window.scrollX,
    y: window.scrollY,
  });

  useLayoutEffect(() => {
    const handler = () =>
      setState({
        x: window.scrollX,
        y: window.scrollY,
      });

    window.addEventListener("scroll", handler);
    return () => {
      window.removeEventListener("scroll", handler);
    };
  }, []);

  return state;
}

type ResizeObserverSingleCallback = (entry: ResizeObserverEntry, observer: ResizeObserver) => void;

const resizeCallbacks = new WeakMap<Element, ResizeObserverSingleCallback>();
const resizeObserver = new ResizeObserver((entries, observer) => {
  for (const entry of entries) {
    const callback = resizeCallbacks.get(entry.target);
    callback?.(entry, observer);
  }
});

/** Resize observer hook inspired by @react-hooks/resize-observer. */
export function useResizeObserver<T extends HTMLElement>(
  target: RefObject<T>,
  callback: ResizeObserverSingleCallback
): ResizeObserver {
  useLayoutEffect(() => {
    const element = target.current;
    if (!element) return;

    resizeCallbacks.set(element, callback);
    return () => {
      resizeCallbacks.delete(element);
    };
  }, [target.current, callback]);

  useLayoutEffect(() => {
    const element = target.current;
    if (!element) return;

    resizeObserver.observe(element);
    return () => resizeObserver.unobserve(element);
  }, [target.current]);

  return resizeObserver;
}
