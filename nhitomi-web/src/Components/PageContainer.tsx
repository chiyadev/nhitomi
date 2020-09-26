import React, { ReactNode, useLayoutEffect } from "react";
import { animated, useSpring } from "react-spring";
import { usePageState } from "../state";
import { useErrorBoundary } from "preact/hooks";
import { captureException } from "@sentry/react";

export const PageContainer = ({ children, className }: { children?: ReactNode; className?: string }) => {
  const [error] = useErrorBoundary((e) => {
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
    return (
      <div>
        <div>There was a problem while displaying this page. Please try again later!</div>

        <br />
        <div className="text-sm whitespace-pre">
          <code>{error.stack}</code>
        </div>
      </div>
    );
  }

  return (
    <animated.div style={style} className={className}>
      {children}
    </animated.div>
  );
};
