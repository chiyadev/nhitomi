import React, { useLayoutEffect, useMemo, useState } from "react";
import { NavigationArgs, useNavigator, useQueryState } from "../state";
import { PrefetchGenerator, PrefetchLink, TypedPrefetchLinkProps, usePostfetch } from "../Prefetch";
import { ClientInfo, useClientInfo } from "../ClientManager";
import { useTabTitle } from "../TitleSetter";
import { useLocalized } from "../LocaleManager";
import { parseOAuthState, stringifyOAuthState, useXsrfToken } from "./oauth";
import { FormattedMessage } from "react-intl";
import { DiscordColor, DiscordOutlined } from "../Components/Icons/DiscordOutlined";
import { FilledButton } from "../Components/FilledButton";
import { Disableable } from "../Components/Disableable";
import { TwitterColor, TwitterOutlined } from "../Components/Icons/TwitterOutlined";
import { animated, useSpring } from "react-spring";
import GitHubButton from "react-github-btn";
import { Anchor } from "../Components/Anchor";

export type PrefetchResult = { info: ClientInfo; state: string };
export type PrefetchOptions = { redirect?: NavigationArgs };

export const useAuthenticationPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({
  mode,
  redirect: targetRedirect,
}) => {
  const { info } = useClientInfo();
  const [currentState] = useQueryState<string>("replace", "state");
  const navigator = useNavigator();
  const [xsrf] = useXsrfToken();

  let state: string;

  if (targetRedirect) {
    state = stringifyOAuthState({
      xsrf,
      redirect: navigator.evaluate(targetRedirect),
    });
  } else if (mode === "postfetch" && currentState) {
    const { redirect } = parseOAuthState(currentState);
    state = stringifyOAuthState({ xsrf, redirect });
  } else {
    state = stringifyOAuthState({ xsrf, redirect: { path: "/" } });
  }

  return {
    destination: {
      path: "/auth",
      query: { state },
    },

    // info is always assumed to be up-to-date
    fetch: async () => ({ info, state }),
  };
};

export const AuthenticationLink = ({
  redirect,
  ...props
}: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useAuthenticationPrefetch} options={{ redirect }} {...props} />
);

export const Authentication = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useAuthenticationPrefetch, options);

  if (!result) return null;

  return <Loaded {...result} />;
};

function appendState(url: string, state: string) {
  const u = new URL(url);
  u.searchParams.append("state", state);
  return u.href;
}

const Loaded = ({ info: { discordOAuthUrl }, state }: PrefetchResult) => {
  useTabTitle(useLocalized("pages.authentication.title"));

  const navigator = useNavigator();
  const { info } = useClientInfo();

  useLayoutEffect(() => {
    // redirect immediately if already authenticated
    if (info.authenticated) {
      const { redirect } = parseOAuthState(state);

      navigator.navigate("replace", { state: {}, ...redirect });
    }
  }, [info.authenticated, navigator, state]);

  const logoStyle = useSpring({
    from: { opacity: 0, transform: "scale(0.9)" },
    to: { opacity: 1, transform: "scale(1)" },
  });

  const infoStyle = useSpring({
    from: { opacity: 0, marginTop: -5 },
    to: { opacity: 1, marginTop: 0 },
  });

  return (
    <>
      <animated.img
        style={logoStyle}
        alt="logo"
        className="w-48 h-48 pointer-events-none select-none mx-auto mb-4 mt-8"
        src="/logo-192x192.png"
      />

      <animated.div style={infoStyle} className="space-y-8">
        {useMemo(
          () => (
            <div className="space-y-1">
              <div className="text-center text-2xl font-bold">nhitomi</div>
              <div className="text-center text-sm text-gray-darker">
                <FormattedMessage id="pages.authentication.tagline" />
              </div>
            </div>
          ),
          []
        )}

        <div className="flex flex-col items-center space-y-1">
          {useMemo(
            () => (
              <Anchor href={appendState(discordOAuthUrl, state)}>
                <FilledButton className="text-sm" color={DiscordColor} icon={<DiscordOutlined />}>
                  <FormattedMessage id="pages.authentication.connect.discord" />
                </FilledButton>
              </Anchor>
            ),
            [discordOAuthUrl, state]
          )}

          {useMemo(
            () => (
              <Disableable disabled>
                <FilledButton className="text-sm" color={TwitterColor} icon={<TwitterOutlined />}>
                  <FormattedMessage id="pages.authentication.connect.twitter" />
                </FilledButton>
              </Disableable>
            ),
            []
          )}
        </div>

        <GitHubButtons />
      </animated.div>
    </>
  );
};

const GitHubButtons = () => {
  const [hover, setHover] = useState(false);

  const style = useSpring({
    opacity: hover ? 1 : 0.5,
  });

  return (
    <animated.div
      style={style}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      className="flex justify-center space-x-1 opacity-75"
    >
      <GitHubButton
        href="https://github.com/chiyadev/nhitomi/subscription"
        data-icon="octicon-eye"
        data-show-count={true}
      >
        Watch
      </GitHubButton>
      <GitHubButton
        href="https://github.com/chiyadev/nhitomi"
        data-icon="octicon-star"
        data-show-count={true}
      >
        Star
      </GitHubButton>
      <GitHubButton
        href="https://github.com/chiyadev/nhitomi/fork"
        data-icon="octicon-repo-forked"
        data-show-count={true}
      >
        Fork
      </GitHubButton>
    </animated.div>
  );
};
