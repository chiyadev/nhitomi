import React, { ComponentProps, useLayoutEffect, useRef } from "react";
import { SettingsFocus } from ".";
import { useQueryState } from "../state";
import { css, cx } from "emotion";

export const SettingsFocusContainer = ({
  focus,
  className,
  ...props
}: ComponentProps<"div"> & { focus: SettingsFocus }) => {
  const ref = useRef<HTMLDivElement>(null);
  const [currentFocus] = useQueryState<SettingsFocus>("replace", "focus");

  useLayoutEffect(() => {
    if (currentFocus === focus) {
      ref.current?.scrollIntoView();
    }
  }, [currentFocus, focus, ref]);

  return (
    <div
      ref={ref} // p-2 -m-2 to make border outside the container
      className={cx(
        className,
        {
          "rounded border border-blue border-opacity-50 p-2 -m-2": currentFocus === focus,
        },
        css`
          scroll-margin: 1em;
        `
      )}
      {...props}
    />
  );
};
