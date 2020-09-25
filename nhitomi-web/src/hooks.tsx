import { DependencyList, RefObject, useLayoutEffect, useState } from "react";
import { useAsyncFn } from "react-use";
import { FnReturningPromise } from "react-use/lib/util";
import { ResizeObserver, ResizeObserverEntry } from "@juggle/resize-observer";

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
    const handler = () => {
      setState({
        width: window.innerWidth,
        height: window.innerHeight,
      });
    };

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
    const handler = () => {
      setState({
        x: window.scrollX,
        y: window.scrollY,
      });
    };

    window.addEventListener("scroll", handler);
    return () => {
      window.removeEventListener("scroll", handler);
    };
  }, []);

  return state;
}

const resizeCallbacks = new WeakMap<Element, (entry: ResizeObserverEntry) => void>();
const resizeObserver = new ResizeObserver((entries) => {
  // https://stackoverflow.com/a/58701523/13160620
  for (const entry of entries) {
    const callback = resizeCallbacks.get(entry.target);
    callback?.(entry);
  }
});

export function useSize<T extends Element>(target: RefObject<T>) {
  const [size, setSize] = useState<ResizeObserverEntry["contentRect"] | undefined>(() =>
    target.current?.getBoundingClientRect()
  );

  useLayoutEffect(() => {
    const element = target.current;
    if (!element) return;

    resizeCallbacks.set(element, (entry) => setSize(entry.contentRect));
    return () => {
      resizeCallbacks.delete(element);
    };
  }, [target.current]);

  useLayoutEffect(() => {
    const element = target.current;
    if (!element) return;

    resizeObserver.observe(element);
    return () => resizeObserver.unobserve(element);
  }, [target.current]);

  return size;
}
