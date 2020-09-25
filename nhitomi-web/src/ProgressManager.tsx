import React, { createContext, ReactNode, useContext, useLayoutEffect, useMemo, useState } from "react";
import { configureProgress, stopProgress, startProgress } from "./progress";
import { AnimationMode } from "./ConfigManager";

const ProgressContext = createContext<{
  begin: () => void;
  end: () => void;

  mode: AnimationMode;
  setMode: (mode: AnimationMode) => void;
}>(undefined as any);

export function useProgress() {
  return useContext(ProgressContext);
}

export const ProgressManager = ({ children }: { children?: ReactNode }) => {
  // do not rely on useConfig('animation') to allow for code splitting
  // see AnimationSetter, where this state is actually synchronized with the config entry
  const [mode, setMode] = useState<AnimationMode>("normal");

  useLayoutEffect(() => configureProgress(mode), [mode]);

  return (
    <ProgressContext.Provider
      value={useMemo(
        () => ({
          begin: startProgress,
          end: stopProgress,

          mode,
          setMode,
        }),
        [mode]
      )}
    >
      {children}
    </ProgressContext.Provider>
  );
};
