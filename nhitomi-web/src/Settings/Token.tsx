import React, { useState } from "react";
import { FormattedMessage } from "react-intl";
import { SettingsFocusContainer } from "./SettingsFocusContainer";
import { useConfig } from "../ConfigManager";
import { cx } from "emotion";
import { LinkOutlined } from "@ant-design/icons";
import { getColor } from "../theme";
import { Anchor } from "../Components/Anchor";
import { trackEvent } from "../umami";

export const Token = () => {
  const [token] = useConfig("token");
  const [visible, setVisible] = useState(false);

  return (
    <SettingsFocusContainer focus="token">
      <div>
        <FormattedMessage id="pages.settings.user.token.name" />
      </div>
      <div className="text-sm text-gray-darker">
        <FormattedMessage id="pages.settings.user.token.description" />
      </div>
      <br />

      <div className="mb-2">
        <Anchor target="_blank" className="text-sm text-blue" href="https://github.com/chiyadev/nhitomi/wiki/API">
          <LinkOutlined /> <FormattedMessage id="pages.settings.user.token.docs" />
        </Anchor>
      </div>

      <div className="break-all">
        <FormattedMessage
          id="pages.settings.user.token.token"
          values={{
            token: (
              <span
                onClick={() => {
                  trackEvent("action", "settingsViewToken");
                  setVisible(true);
                }}
                className={cx("bg-gray-darkest px-1 rounded", {
                  "cursor-pointer": !visible,
                })}
                style={{
                  color: visible ? undefined : getColor("transparent").rgb,
                }}
              >
                {token}
              </span>
            ),
          }}
        />
      </div>

      {visible && (
        <div className="text-sm text-red">
          <FormattedMessage id="pages.settings.user.token.warning" />
        </div>
      )}
    </SettingsFocusContainer>
  );
};
