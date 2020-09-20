import { PrefetchGenerator, PrefetchLink, TypedPrefetchLinkProps, usePostfetch } from "../Prefetch";
import { useQueryState } from "../state";
import React, { useLayoutEffect, useMemo } from "react";
import { PageContainer } from "../Components/PageContainer";
import { Container } from "../Components/Container";
import { Loading3QuartersOutlined } from "@ant-design/icons";
import { FormattedMessage } from "react-intl";
import { stringify } from "qs";
import { useConfig } from "../ConfigManager";

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
        name: "nhitomi Android",
        redirectUri: "com.ihsankl.nhitomi:auth/success",
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

const Loaded = ({ client, state }: PrefetchResult) => {
  const [token] = useConfig("token");
  const redirectUri = useMemo(() => {
    const query = stringify({ token, state }, { addQueryPrefix: true });

    return client.redirectUri + query;
  }, []);

  useLayoutEffect(() => window.location.replace(redirectUri), []);

  return (
    <Container className="p-4">
      <div className="space-x-2">
        <Loading3QuartersOutlined className="animate-spin" />

        <span>
          <FormattedMessage
            id="pages.authentication.redirect.progress"
            values={{ name: <span className="font-bold">{client.name}</span> }}
          />
        </span>
      </div>
    </Container>
  );
};
