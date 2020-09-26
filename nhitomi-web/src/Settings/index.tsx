import React, { ReactNode, useMemo } from "react";
import { PrefetchGenerator, PrefetchLink, TypedPrefetchLinkProps, usePostfetch } from "../Prefetch";
import { ClientInfo, useClientInfo, usePermissions } from "../ClientManager";
import { Container } from "../Components/Container";
import { FormattedMessage } from "react-intl";
import { Language } from "./Language";
import { Animation } from "./Animation";
import { SettingsFocusContainer } from "./SettingsFocusContainer";
import { useQueryState } from "../state";
import { MacCommandFilled, PictureOutlined, ReadOutlined, ToolOutlined, UserOutlined } from "@ant-design/icons";
import { PageContainer } from "../Components/PageContainer";
import { Blur } from "./Blur";
import { Shortcuts } from "./Shortcuts";
import { useTabTitle } from "../TitleSetter";
import { useLocalized } from "../LocaleManager";
import { PreferEnglishName } from "./PreferEnglishName";
import { Debug } from "./Debug";
import { Server } from "./Server";
import { UserPermissions } from "nhitomi-api";
import { Account } from "./Account";
import { Token } from "./Token";

export type PrefetchResult = ClientInfo;
export type PrefetchOptions = { focus?: SettingsFocus };

export type SettingsStructure = {
  internal: {
    debug: true;
    server: true;
  };
  user: {
    account: true;
    token: true;
  };
  appearance: {
    language: true;
    animation: true;
    blur: true;
  };
  reader: {
    preferEnglishName: true;
  };
  keyboard: {
    shortcuts: true;
  };
};

export type SettingsSection = keyof SettingsStructure;
export type SettingsItem = {
  [key in SettingsSection]: keyof SettingsStructure[key];
}[SettingsSection];
export type SettingsFocus = SettingsSection | SettingsItem;

export const useSettingsPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({
  mode,
  focus: targetFocus,
}) => {
  const { fetchInfo } = useClientInfo();
  const [currentFocus] = useQueryState<SettingsFocus>("replace", "focus");

  const focus = targetFocus || (mode === "postfetch" && currentFocus) || undefined;

  return {
    destination: {
      path: "/settings",
      query: { focus },
    },

    restoreScroll: !focus,
    fetch: fetchInfo,
  };
};

export const SettingsLink = ({ focus, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useSettingsPrefetch} options={{ focus }} {...props} />
);

export const Settings = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useSettingsPrefetch, {
    requireAuth: true,
    ...options,
  });

  if (!result) return null;

  return (
    <PageContainer>
      <Loaded />
    </PageContainer>
  );
};

const Loaded = () => {
  const permissions = usePermissions();

  useTabTitle(useLocalized("pages.settings.title"));

  return useMemo(
    () => (
      <Container className="divide-y divide-gray-darkest">
        <div className="p-4">
          <div className="text-2xl">
            <FormattedMessage id="pages.settings.title" />
          </div>
          <div className="text-sm text-gray-darker">
            <FormattedMessage id="pages.settings.subtitle" />
          </div>
        </div>

        <div className="p-4 space-y-12">
          <Section
            type="user"
            name={
              <span>
                <UserOutlined /> <FormattedMessage id="pages.settings.user.header" />
              </span>
            }
            items={[
              {
                item: "account",
                node: <Account />,
              },
              {
                item: "token",
                node: <Token />,
              },
            ]}
          />

          <Section
            type="appearance"
            name={
              <span>
                <PictureOutlined /> <FormattedMessage id="pages.settings.appearance.header" />
              </span>
            }
            items={[
              {
                item: "language",
                node: <Language />,
              },
              {
                item: "animation",
                node: <Animation />,
              },
              {
                item: "blur",
                node: <Blur />,
              },
            ]}
          />

          <Section
            type="reader"
            name={
              <span>
                <ReadOutlined /> <FormattedMessage id="pages.settings.reader.header" />
              </span>
            }
            items={[
              {
                item: "preferEnglishName",
                node: <PreferEnglishName />,
              },
            ]}
          />

          <Section
            type="keyboard"
            name={
              <span>
                <MacCommandFilled /> <FormattedMessage id="pages.settings.keyboard.header" />
              </span>
            }
            items={[
              {
                item: "shortcuts",
                node: <Shortcuts />,
              },
            ]}
          />

          <Section
            type="internal"
            name={
              <span>
                <ToolOutlined /> Internal
              </span>
            }
            items={[
              {
                item: "debug",
                node: process.env.NODE_ENV === "development" && <Debug />,
              },
              {
                item: "server",
                node: permissions.hasPermissions(UserPermissions.ManageServer) && <Server />,
              },
            ]}
          />
        </div>
      </Container>
    ),
    [permissions]
  );
};

const Section = ({
  name,
  type,
  items,
  className,
}: {
  name?: ReactNode;
  type: SettingsSection;
  items: { item: SettingsItem; node: ReactNode }[];
  className?: string;
}) => {
  items = items?.filter((c) => c.node);

  if (!items.length) return null;

  return (
    <SettingsFocusContainer focus={type} className={className}>
      <div className="text-sm text-gray-darker font-bold">{name}</div>

      <div className="divide-y divide-gray-darkest">
        {items.map(({ item, node }) => (
          <div key={item} className="py-4">
            {node}
          </div>
        ))}
      </div>
    </SettingsFocusContainer>
  );
};
