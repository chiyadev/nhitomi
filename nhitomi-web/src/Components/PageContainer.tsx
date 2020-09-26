import React, { ReactNode, useLayoutEffect } from "react";
import { animated, useSpring } from "react-spring";
import { usePageState } from "../state";
import { useErrorBoundary } from "preact/hooks";
import { captureException } from "@sentry/react";
import { useNotify } from "../NotificationManager";

export const PageContainer = ({ children, className }: { children?: ReactNode; className?: string }) => {
  const { notifyError } = useNotify();

  const [error] = useErrorBoundary((e) => {
    notifyError(e, "There was a problem while displaying this page.");
    captureException(e);
  });

  const [shown, setShown] = usePageState<boolean>("pageShown");

  // prevents fade-in on back navigate
  useLayoutEffect(() => setShown(true), [setShown]);

  const style = useSpring({
    from: { opacity: shown ? 1 : 0 },
    to: { opacity: 1 },
  });

  if (error) {
    return null;
  }

  return (
    <animated.div style={style} className={className}>
      {children}
    </animated.div>
  );
};
