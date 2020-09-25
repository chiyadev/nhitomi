import { DependencyList, RefObject, useLayoutEffect, useState } from "react";
import { useAsyncFn, useLatest } from "react-use";
import { FnReturningPromise } from "react-use/lib/util";
import ResizeObserver from "resize-observer-polyfill";

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

const resizeCallbacks = new Set<ResizeObserverCallback>();
const resizeObserver = new ResizeObserver((entries, observer) => {
  // https://stackoverflow.com/a/58701523/13160620
  requestAnimationFrame(() => {
    resizeCallbacks.forEach((callback) => callback(entries, observer));
  });
});

/** Resize observer hook inspired by @react-hooks/resize-observer. */
export function useResizeObserver<T extends HTMLElement>(
  target: RefObject<T> | T | null,
  callback: (entry: ResizeObserverEntry, observer: ResizeObserver) => void
): ResizeObserver {
  const storedCallback = useLatest(callback);

  useLayoutEffect(() => {
    let unsubscribed = false;

    const callback = (entries: ResizeObserverEntry[], observer: ResizeObserver) => {
      if (unsubscribed) return;
      const targetEl = target && "current" in target ? target.current : target;

      for (let i = 0; i < entries.length; i++) {
        const entry = entries[i];

        if (entry.target === targetEl) {
          storedCallback.current(entry, observer);
        }
      }
    };

    resizeCallbacks.add(callback);
    return () => {
      unsubscribed = true;
      resizeCallbacks.delete(callback);
    };
  }, [target, storedCallback]);

  useLayoutEffect(() => {
    const targetEl = target && "current" in target ? target.current : target;
    if (!targetEl) return;

    resizeObserver.observe(targetEl);
    return () => resizeObserver.unobserve(targetEl);
  }, [target]);

  return resizeObserver;
}
