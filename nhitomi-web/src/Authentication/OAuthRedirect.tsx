import { PrefetchGenerator, PrefetchLink, TypedPrefetchLinkProps, usePostfetch } from "../Prefetch";
import { useQueryState } from "../state";
import React, { useMemo, useState } from "react";
import { PageContainer } from "../Components/PageContainer";
import { Container } from "../Components/Container";
import { Loading3QuartersOutlined } from "@ant-design/icons";
import { FormattedMessage } from "react-intl";
import { stringify } from "qs";
import { useConfig } from "../ConfigManager";
import { Anchor } from "../Components/Anchor";
import { useInterval } from "react-use";

type OAuthClientInfo = {
  id: string;
  name: string;
  redirectUri: string;
};

// there is no actual server-side support for oauth clients of nhitomi yet
// if oauth will ever be properly implemented, this information should be retrieved from the api
async function getOAuthClient(id: string): Promise<OAuthClientInfo | null> {
  switch (id) {
    case "Jg9YpDm6":
      return {
        id,
        name: "nhitomi for Android",
        redirectUri: "com.ihsankl.nhitomi://nhitomi/auth/success",
      };

    default:
      return null;
  }
}

export type PrefetchResult = { client: OAuthClientInfo; state?: string };
export type PrefetchOptions = { id: string; state?: string };

export const useOAuthRedirectPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({
  mode,
  id,
  state: targetState,
}) => {
  const [currentState] = useQueryState<string>("replace", "state");
  const state = targetState || (mode === "postfetch" && currentState) || undefined;

  return {
    destination: {
      path: `/oauth/redirect/${id}`,
      query: { state },
    },

    fetch: async () => {
      const client = await getOAuthClient(id);

      if (!client) throw Error(`Could not find OAuth client '${id}'.`);

      return { client, state };
    },
  };
};

export const OAuthRedirectLink = ({ id, state, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useOAuthRedirectPrefetch} options={{ id: id, state }} {...props} />
);

export const OAuthRedirect = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useOAuthRedirectPrefetch, {
    requireAuth: true,
    ...options,
  });

  if (!result) return null;

  return (
    <PageContainer>
      <Loaded {...result} />
    </PageContainer>
  );
};

const redirectRetries = 5;

const Loaded = ({ client, state }: PrefetchResult) => {
  const [token] = useConfig("token");
  const redirectUri = useMemo(() => {
    const query = stringify({ token, state }, { addQueryPrefix: true });

    return client.redirectUri + query;
  }, []);

  const [retry, setRetry] = useState(0);

  useInterval(
    () => {
      window.location.replace(redirectUri);
      setRetry((retry) => retry + 1);
    },
    retry < redirectRetries ? 1000 : null
  );

  return (
    <Container className="p-4 space-y-4">
      <div className="space-x-2">
        <Loading3QuartersOutlined className="animate-spin" />

        <span>
          <FormattedMessage
            id="pages.authentication.redirect.progress"
            values={{
              name: (
                <Anchor className="font-bold" href={redirectUri}>
                  {client.name}
                </Anchor>
              ),
            }}
          />
        </span>
      </div>

      {retry >= redirectRetries && (
        <div className="text-sm text-gray-darker">
          <FormattedMessage
            id="pages.authentication.redirect.failed.text"
            values={{
              link: (
                <Anchor className="text-blue" href={redirectUri}>
                  <FormattedMessage id="pages.authentication.redirect.failed.link" />
                </Anchor>
              ),
            }}
          />
        </div>
      )}
    </Container>
  );
};
