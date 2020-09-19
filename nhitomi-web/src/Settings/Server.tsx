import React, { useMemo, useRef } from "react";
import { SettingsFocusContainer } from "./SettingsFocusContainer";
import { Loading3QuartersOutlined } from "@ant-design/icons";
import { useAsync } from "../hooks";
import { useClient } from "../ClientManager";
import { useNotify } from "../NotificationManager";
import { ConfigEntry } from "nhitomi-api";
import { cx } from "emotion";
import { Input } from "../Components/Input";
import { useProgress } from "../ProgressManager";
import { usePageState } from "../state";

export const Server = () => {
  const client = useClient();
  const { begin, end } = useProgress();
  const { notifyError } = useNotify();
  const [config, setConfig] = usePageState<ConfigEntry[]>("config");

  useAsync(async () => {
    if (config) return;

    try {
      setConfig(await client.internal.getServerConfig());
    } catch (e) {
      notifyError(e);
    }
  }, []);

  const updatingRef = useRef<HTMLDivElement>(null);
  const [updatingKey, setUpdatingKey] = usePageState("updatingKey", "");
  const [updatingValue, setUpdatingValue] = usePageState("updatingValue", "");

  const submitChange = async () => {
    begin();

    try {
      setConfig(
        await client.internal.setServerConfig({
          setConfigRequest: {
            key: updatingKey,
            value: updatingValue,
          },
        })
      );

      setUpdatingKey("");
      setUpdatingValue("");
    } catch (e) {
      notifyError(e);
    } finally {
      end();
    }
  };

  return (
    <SettingsFocusContainer focus="server">
      <div>Server configuration</div>
      <br />

      <div className="break-words">
        {useMemo(
          () =>
            config ? (
              config.map(({ key, value }) => (
                <div
                  key={key}
                  className={cx({
                    "rounded-sm bg-gray-darkest font-bold":
                      key.toLowerCase() === updatingKey.toLowerCase(),
                  })}
                >
                  <code
                    className="text-sm text-gray-darker cursor-pointer"
                    onClick={() => {
                      setUpdatingKey(key);
                      setUpdatingValue(value);

                      updatingRef.current?.scrollIntoView({
                        block: "nearest",
                        inline: "nearest",
                      });
                    }}
                  >
                    <span>{key}: </span>
                  </code>

                  <code
                    className={cx("text-sm", {
                      "text-blue": !isNaN(parseInt(value)),
                      "text-orange": typeof parseBoolean(value) === "boolean",
                      "text-green": !!parseURL(value),
                    })}
                  >
                    {value}
                  </code>
                </div>
              ))
            ) : (
              <Loading3QuartersOutlined className="animate-spin" />
            ),
          [config, setUpdatingKey, setUpdatingValue, updatingKey]
        )}
      </div>
      <br />

      <div ref={updatingRef} className="flex flex-row">
        <Input
          className="w-32"
          placeholder="Key"
          value={updatingKey}
          setValue={setUpdatingKey}
          onSubmit={submitChange}
        />
        <div className="px-2 py-1">:</div>
        <Input
          className="flex-1"
          placeholder="Value"
          value={updatingValue}
          setValue={setUpdatingValue}
          onSubmit={submitChange}
        />
      </div>
    </SettingsFocusContainer>
  );
};

function parseBoolean(s: string): boolean | undefined {
  s = s.toLowerCase();

  if (s === "true") return true;
  if (s === "false") return false;
}

function parseURL(s: string): URL | undefined {
  try {
    return new URL(s);
  } catch {
    // ignored
  }
}
