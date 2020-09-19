import React, {
  Dispatch,
  FocusEvent,
  KeyboardEvent,
  ReactNode,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { animated, useSpring } from "react-spring";
import { Color, getColor } from "../theme";
import { css, cx } from "emotion";
import { CloseOutlined } from "@ant-design/icons";
import { useShortcut } from "../shortcut";

export const Input = ({
  value = "",
  setValue,
  type = "input",
  color = getColor("gray", "darkest").opacity(0.5),
  selectionColor = getColor("blue").opacity(0.5),
  autoFocus,
  placeholder,
  allowClear,
  padding = true,
  className,
  onSubmit,
  onKeyDown,
  onFocus,
  onBlur,
  help,
}: {
  value?: string;
  setValue?: Dispatch<string>;
  type?: "input" | "textarea";
  color?: Color;
  selectionColor?: Color;
  autoFocus?: boolean;
  placeholder?: ReactNode;
  allowClear?: boolean;
  padding?: boolean;
  className?: string;
  onSubmit?: (value: string) => void;
  onKeyDown?: (
    e: KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => void;
  onFocus?: (e: FocusEvent<HTMLInputElement | HTMLTextAreaElement>) => void;
  onBlur?: (e: FocusEvent<HTMLInputElement | HTMLTextAreaElement>) => void;
  help?: ReactNode;
}) => {
  const ref = useRef<HTMLInputElement & HTMLTextAreaElement>(null);

  useShortcut("cancelKey", () => ref.current?.blur(), ref);

  useLayoutEffect(() => {
    if (autoFocus) ref.current?.focus();
  }, [autoFocus]);

  const [hover, setHover] = useState(false);
  const [focus, setFocus] = useState(false);

  const inputStyle = useSpring({
    boxShadow: `inset 0 0 0 1px ${
      color.tint(focus || hover ? 0.25 : 0.125).rgb
    }`,
    backgroundColor: color.tint(focus ? 0.125 : 0).rgb,
  });

  const placeholderStyle = useSpring({
    color: color.tint(0.5).opacity(focus ? 1 : 0.5).rgb,
  });

  const helpStyle = useSpring({
    color: color.hex,
    opacity: help ? 1 : 0,
  });

  const [clearHover, setClearHover] = useState(false);
  const clearVisible = allowClear && value && !focus;
  const clearStyle = useSpring(
    clearVisible
      ? {
          opacity: clearHover ? 0.75 : 0.5,
          transform: clearHover ? "scale(1.1)" : "scale(1)",
        }
      : {
          opacity: 0,
          transform: "scale(1)",
        }
  );

  const input = useMemo(() => {
    switch (type) {
      case "input":
        return (
          <input
            ref={ref}
            className={cx(
              "w-full",
              { "px-2 py-1": padding },
              css`
                background: transparent;

                &::selection {
                  background: ${selectionColor.hex};
                }
              `
            )}
            value={value}
            autoFocus={autoFocus}
            onChange={({ target: { value } }) => setValue?.(value)}
            onMouseEnter={() => setHover(true)}
            onMouseLeave={() => setHover(false)}
            onFocus={(e) => {
              setFocus(true);
              return onFocus?.(e);
            }}
            onBlur={(e) => {
              setFocus(false);
              return onBlur?.(e);
            }}
            onKeyDown={(e) => {
              // enter
              if (onSubmit && e.keyCode === 13) {
                onSubmit(value);
                e.preventDefault();
              }

              onKeyDown?.(e);
            }}
          />
        );

      case "textarea":
        return (
          <textarea
            ref={ref}
            className={cx(
              "w-full",
              { "px-2 py-1": padding },
              css`
                background: transparent;
                min-height: 3em;

                &::selection {
                  background: ${selectionColor.hex};
                }
              `
            )}
            value={value}
            autoFocus={autoFocus}
            onChange={({ target: { value } }) => setValue?.(value)}
            onMouseEnter={() => setHover(true)}
            onMouseLeave={() => setHover(false)}
            onFocus={(e) => {
              setFocus(true);
              return onFocus?.(e);
            }}
            onBlur={(e) => {
              setFocus(false);
              return onBlur?.(e);
            }}
            onKeyDown={(e) => {
              // enter
              if (onSubmit && e.keyCode === 13 && e.ctrlKey) {
                onSubmit(value);
                e.preventDefault();
              }

              onKeyDown?.(e);
            }}
          />
        );
    }
  }, [
    autoFocus,
    onBlur,
    onFocus,
    onKeyDown,
    onSubmit,
    padding,
    selectionColor.hex,
    setValue,
    type,
    value,
  ]);

  return (
    <div className={cx("inline-flex flex-col text-white", className)}>
      <animated.div
        className="w-full relative rounded-sm overflow-hidden"
        style={inputStyle}
      >
        {input}

        {!value && (
          <animated.div
            style={placeholderStyle}
            className="absolute top-0 left-0 w-full px-2 py-1 max-h-full align-top pointer-events-none truncate"
            children={placeholder}
          />
        )}

        {allowClear && (
          <animated.div
            style={clearStyle}
            className={cx(
              "absolute top-0 right-0 px-2 py-1 cursor-pointer h-8",
              { "pointer-events-none": !clearVisible }
            )}
            onMouseEnter={() => setClearHover(true)}
            onMouseLeave={() => setClearHover(false)}
            onMouseDown={() => {
              setValue?.("");
              setTimeout(() => ref.current?.focus());
            }}
          >
            <CloseOutlined className="text-xs text-gray-lighter" />
          </animated.div>
        )}
      </animated.div>

      {help && (
        <animated.div
          style={helpStyle}
          className="text-sm truncate mt-1"
          children={help}
        />
      )}
    </div>
  );
};
