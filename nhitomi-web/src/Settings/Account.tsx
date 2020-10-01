import React from "react";
import { FormattedMessage } from "react-intl";
import { SettingsFocusContainer } from "./SettingsFocusContainer";
import { useConfig } from "../ConfigManager";
import { useClientInfo } from "../ClientManager";
import { LogoutOutlined } from "@ant-design/icons";
import { FlatButton } from "../Components/FlatButton";
import { trackEvent } from "../track";

export const Account = () => {
  const { info, setInfo } = useClientInfo();
  const [, setToken] = useConfig("token");

  if (!info.user) return null;

  return (
    <SettingsFocusContainer focus="account">
      <div>
        <FormattedMessage id="pages.settings.user.account.name" />
      </div>
      <div className="text-sm text-gray-darker">
        <FormattedMessage id="pages.settings.user.account.description" values={{ name: info.user.username }} />
      </div>
      <br />

      <FlatButton
        icon={<LogoutOutlined />}
        onClick={() => {
          trackEvent("action", "settingsLogOut");

          setToken(undefined);
          setInfo({ ...info, user: undefined });
        }}
      >
        <FormattedMessage id="pages.settings.user.account.logout" />
      </FlatButton>
    </SettingsFocusContainer>
  );
};
