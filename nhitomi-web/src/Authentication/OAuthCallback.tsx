import { useMemo } from "react";
import { parseOAuthState, useXsrfToken } from "./oauth";
import { useNavigator, useQuery, useQueryState } from "../state";
import { usePrefetch } from "../Prefetch";
import { useAuthenticationPrefetch } from ".";
import { useAsync } from "../hooks";
import { useProgress } from "../ProgressManager";
import { useNotify } from "../NotificationManager";
import { useClient, useClientInfo } from "../ClientManager";
import { AuthenticateResponse } from "nhitomi-api";
import { useConfig } from "../ConfigManager";

export type OAuthService = "discord";

export const OAuthCallback = ({ service }: { service: OAuthService }) => {
  const client = useClient();
  const navigator = useNavigator();
  const { info, setInfo } = useClientInfo();
  const { begin, end } = useProgress();
  const { notifyError } = useNotify();

  const [query] = useQuery();
  const [state] = useQueryState<string>("replace", "state");

  const [validXsrf, resetXsrf] = useXsrfToken();
  const [, setToken] = useConfig("token");
  const [, navigateAuth] = usePrefetch(useAuthenticationPrefetch, {
    redirect: useMemo(() => (state ? parseOAuthState(state).redirect : { path: "/" }), [state]),
  });

  useAsync(async () => {
    begin();

    try {
      if (!state) throw Error("Missing OAuth state query.");

      const { xsrf: currentXsrf, redirect } = parseOAuthState(state);

      // verify xsrf token
      if (currentXsrf !== validXsrf) throw Error("Invalid XSRF token.");

      let result: AuthenticateResponse;

      switch (service) {
        case "discord":
          result = await client.user.authenticateUserDiscord({
            authenticateDiscordRequest: { code: query.code as string },
          });
          break;

        default:
          throw Error(`Unsupported OAuth service '${service}'.`);
      }

      setInfo({
        ...info,
        authenticated: true,
        user: result.user,
      });

      setToken(result.token);

      navigator.navigate("replace", { state: {}, ...redirect });
    } catch (e) {
      notifyError(e);

      await navigateAuth("replace");
    } finally {
      resetXsrf();
      end();
    }
  }, []);

  return null;
};
