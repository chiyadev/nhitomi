import React, { createContext, ReactNode, useContext, useLayoutEffect, useMemo, useRef, useState } from "react";
import nprogress from "nprogress";
import { AnimationMode } from "./ConfigManager";

import "./Progress.css";

const ProgressContext = createContext<{
  begin: () => void
  end: () => void

  mode: AnimationMode
  setMode: (mode: AnimationMode) => void
}>(undefined as any);

export function useProgress() {
  return useContext(ProgressContext);
}

export const ProgressManager = ({ children }: { children?: ReactNode }) => {
  const count = useRef(0);

  // do not rely on useConfig('animation') to allow for code splitting
  // see AnimationSetter, where this state is actually synchronized with the config entry
  const [mode, setMode] = useState<AnimationMode>("normal");

  useLayoutEffect(() => {
    let easing: string;

    switch (mode) {
      case "normal":
        easing = "ease";
        break;
      case "faster":
        easing = "cubic-bezier(0, 1, 0, 1)";
        break;
      case "none":
        easing = "steps-start";
        break;
    }

    nprogress.configure({
      template: `
        <div class="bar" role="bar">
          <div class="peg"></div>
        </div>
        <div class="spinner" role="spinner">
          <div class="spinner-icon"></div>
        </div>
      `,
      easing
    });
  }, [mode]);

  // timeout prevents flickering when fetching multiple resources in a short time
  const done = useRef<number>();

  return (
    <ProgressContext.Provider
      children={children}
      value={useMemo(() => ({
        begin: () => {
          clearTimeout(done.current);

          if (count.current++ === 0)
            nprogress.start();
        },
        end: () => {
          if (--count.current === 0)
            done.current = window.setTimeout(() => nprogress.done(), 200);
        },
        mode,
        setMode
      }), [mode])} />
  );
};
