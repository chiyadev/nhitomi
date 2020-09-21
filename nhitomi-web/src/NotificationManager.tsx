import React, {
  createContext,
  ReactNode,
  useCallback,
  useContext,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import {
  CheckCircleTwoTone,
  CloseCircleTwoTone,
  CloseOutlined,
  InfoCircleTwoTone,
  WarningTwoTone,
} from "@ant-design/icons";
import { css, cx } from "emotion";
import { animated, useSpring, useTransition } from "react-spring";
import { useLayout } from "./LayoutManager";
import { ValidationError } from "./ClientManager";
import { ColorHue, getColor } from "./theme";
import { exception } from "react-ga";

type AppearanceTypes = "success" | "info" | "error" | "warning";

export const NotificationManager = ({ children }: { children?: ReactNode }) => {
  return (
    <NotifyManager>
      <AlertManager>
        <NotifyDisplay />
        <AlertDisplay />

        {children}
      </AlertManager>
    </NotifyManager>
  );
};

function usePausableTimeout(duration: number, callback: () => void): [() => void, () => void] {
  const timeout = useRef<number>();

  const pause = useCallback(() => clearTimeout(timeout.current), [timeout]);
  const resume = useCallback(() => {
    pause();
    timeout.current = window.setTimeout(callback, duration);
  }, [timeout, pause, callback, duration]);

  useLayoutEffect(() => {
    resume();
    return pause;
  }, [pause, resume]);

  return [pause, resume];
}

function getTypeIcon(type: AppearanceTypes) {
  let Icon: typeof CloseCircleTwoTone;
  let color: ColorHue;

  switch (type) {
    case "error":
      Icon = CloseCircleTwoTone;
      color = "red";
      break;
    case "info":
      Icon = InfoCircleTwoTone;
      color = "blue";
      break;
    case "success":
      Icon = CheckCircleTwoTone;
      color = "green";
      break;
    case "warning":
      Icon = WarningTwoTone;
      color = "orange";
      break;
  }

  return <Icon className="text-lg w-6" twoToneColor={getColor(color).hex} />;
}

const NotifyContext = createContext<{
  items: NotifyItem[];
  remove: (id: number) => void;

  notify: (type: AppearanceTypes, title: ReactNode, description: ReactNode) => void;
  notifyError: (error: Error, title?: ReactNode) => void;
}>(undefined as any);

type NotifyItem = {
  id: number;
  type: AppearanceTypes;
  title: ReactNode;
  description: ReactNode;
};

export function useNotify() {
  return useContext(NotifyContext);
}

const NotifyManager = ({ children }: { children?: ReactNode }) => {
  const [items, setItems] = useState<NotifyItem[]>([]);

  const notify = useCallback((type: AppearanceTypes, title: ReactNode, description: ReactNode) => {
    setItems((items) => [{ id: Math.random(), type, title, description }, ...items]);
  }, []);

  return (
    <NotifyContext.Provider
      value={useMemo(
        () => ({
          items,
          remove: (id) => {
            setItems((items) => items.filter((item) => item.id !== id));
          },

          notify,
          notifyError: (error: any, title) => {
            if (!(error instanceof Error)) error = Error((error as any)?.message || "Unknown error.");

            if (error instanceof ValidationError) {
              notify(
                "error",
                title || error.message,
                <ul className="list-disc list-inside">
                  {error.list.map((problem) => (
                    <li key={problem.field}>
                      <code>{problem.field} </code>
                      <span>{problem.messages.join(" ")}</span>
                    </li>
                  ))}
                </ul>
              );
            } else {
              notify(
                "error",
                title || error.message,
                <>
                  {title && (
                    <div>
                      <code>{error.message}</code>
                    </div>
                  )}
                  <div>
                    <code>{error.stack}</code>
                  </div>
                </>
              );
            }

            exception({
              description: error.message,
              fatal: false,
            });
          },
        }),
        [notify, items]
      )}
    >
      {children}
    </NotifyContext.Provider>
  );
};

const NotifyDisplay = () => {
  const { screen } = useLayout();
  const { items } = useNotify();

  const [transitions] = useTransition(
    items,
    {
      from: { opacity: 0, marginLeft: "1em", marginRight: "-1em" },
      enter: { opacity: 1, marginLeft: "0em", marginRight: "0em" },
      leave: { opacity: 0, marginLeft: "1em", marginRight: "-1em" },
    },
    [items]
  );

  return (
    <div
      className={useMemo(
        () =>
          cx("w-screen fixed top-0 right-0 p-4 z-50 space-y-4", {
            "max-w-lg": screen === "lg",
            "pointer-events-none": !items.length,
          }),
        [screen, items]
      )}
    >
      {transitions((style, item) => (
        <animated.div key={item.id} style={style}>
          <NotifyItemDisplay {...item} />
        </animated.div>
      ))}
    </div>
  );
};

const NotifyItemDisplay = ({ id, type, title, description }: NotifyItem) => {
  const [closeHover, setCloseHover] = useState(false);
  const closeStyle = useSpring({
    transform: closeHover ? "scale(1.1)" : "scale(1)",
  });

  const { remove } = useNotify();
  const close = useCallback(() => remove(id), [remove, id]);
  const [pause, resume] = usePausableTimeout(15000, close);

  return (
    <div
      className="relative w-full rounded overflow-hidden bg-white text-black shadow-lg p-3"
      onMouseEnter={pause}
      onMouseLeave={resume}
    >
      <div className="mb-3">
        {getTypeIcon(type)} {title}
      </div>

      <div className="text-sm overflow-auto">{description}</div>

      <animated.span style={closeStyle} className="absolute top-0 right-0 p-3 text-gray-darker">
        <CloseOutlined
          className="cursor-pointer"
          onClick={close}
          onMouseEnter={() => setCloseHover(true)}
          onMouseLeave={() => setCloseHover(false)}
        />
      </animated.span>
    </div>
  );
};

const AlertContext = createContext<{
  item?: AlertItem;
  remove: (id: number) => void;

  alert: (message: ReactNode, type?: AppearanceTypes) => void;
}>(undefined as any);

type AlertItem = {
  id: number;
  type?: AppearanceTypes;
  message: ReactNode;
};

export function useAlert() {
  return useContext(AlertContext);
}

const AlertManager = ({ children }: { children?: ReactNode }) => {
  const [item, setItem] = useState<AlertItem>();

  return (
    <AlertContext.Provider
      value={useMemo(
        () => ({
          item,
          remove: (id) => setItem((item) => (item?.id === id ? undefined : item)),
          alert: (message, type) => setItem({ id: Math.random(), type, message }),
        }),
        [item]
      )}
    >
      {children}
    </AlertContext.Provider>
  );
};

const AlertDisplay = () => {
  const { screen } = useLayout();
  const { item } = useAlert();

  const [transitions] = useTransition(
    item ? [item] : [],
    {
      from: { opacity: 0, marginTop: "-1em" },
      enter: { opacity: 1, marginTop: "0em" },
      leave: { opacity: 0, marginTop: "-1em" },
    },
    [item]
  );

  return transitions((style, item) => (
    <animated.div
      key={item.id}
      style={style}
      className={useMemo(
        () =>
          cx(
            "fixed top-0 p-4 z-50",
            {
              "max-w-full": screen !== "lg",
              "max-w-lg": screen === "lg",
            },
            css`
              left: 50%;
              transform: translateX(-50%);
            `
          ),
        [screen, item]
      )}
    >
      <AlertItemDisplay {...item} />
    </animated.div>
  ));
};

const AlertItemDisplay = ({ id, message, type }: AlertItem) => {
  const { remove } = useAlert();
  const close = useCallback(() => remove(id), [remove, id]);
  const [pause, resume] = usePausableTimeout(5000, close);

  const messageRef = useRef<HTMLSpanElement>(null);
  const [truncate, setTruncate] = useState(true);

  return (
    <div
      className="inline-flex max-w-full rounded bg-gray-darkest bg-blur text-white shadow-lg p-3 cursor-pointer"
      onMouseEnter={pause}
      onMouseLeave={resume}
      onClick={() => {
        if (truncate) {
          // https://stackoverflow.com/a/10017343/13160620
          const text = messageRef.current;

          if (text && text.offsetWidth < text.scrollWidth) {
            setTruncate(false);
            return;
          }
        }

        close();
      }}
    >
      {type && <span>{getTypeIcon(type)} </span>}

      <span ref={messageRef} className={cx("overflow-hidden break-words", { truncate: truncate })}>
        {message}
      </span>
    </div>
  );
};
