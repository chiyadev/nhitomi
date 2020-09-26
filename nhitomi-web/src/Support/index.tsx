import React, { useMemo } from "react";
import { PrefetchGenerator, PrefetchLink, TypedPrefetchLinkProps, usePostfetch } from "../Prefetch";
import { useClient, useClientInfo } from "../ClientManager";
import { PageContainer } from "../Components/PageContainer";
import { useTabTitle } from "../TitleSetter";
import { useLocalized } from "../LocaleManager";
import { Container } from "../Components/Container";
import { css, cx } from "emotion";
import { HeartFilled } from "@ant-design/icons";
import { FormattedMessage } from "react-intl";
import { MainCard, SupportDescription, ThanksDescription } from "./MainCard";
import { Checkout } from "./Checkout";
import { GetStripeInfoResponse } from "nhitomi-api";
import { useQueryState } from "../state";
import { useNotify } from "../NotificationManager";
import { Benefits } from "./Benefits";

export type PrefetchResult = GetStripeInfoResponse;
export type PrefetchOptions = {};

export const useSupportPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = () => {
  const client = useClient();
  const { notify } = useNotify();
  const [status] = useQueryState<"canceled">("replace", "checkout");

  return {
    destination: {
      path: "/support",
    },

    fetch: async () => {
      switch (status) {
        case "canceled":
          notify(
            "error",
            <FormattedMessage id="pages.support.error.title" />,
            <FormattedMessage id="pages.support.error.canceled" />
          );
          break;
      }

      return await client.info.getStripeInfo();
    },
  };
};

export const SupportLink = ({ ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useSupportPrefetch} options={{}} {...props} />
);

export const Support = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useSupportPrefetch, {
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

const Loaded = (result: PrefetchResult) => {
  useTabTitle(useLocalized("pages.support.title"));

  const { isSupporter } = useClientInfo();

  return (
    <Container className="px-4 space-y-16">
      {useMemo(
        () => (
          <MainCard>
            <div className="space-y-4">
              <div>
                <HeartFilled
                  className={cx(
                    "text-4xl text-pink mr-2",
                    css`
                      transform: rotate(20deg);
                    `
                  )}
                />

                <FormattedMessage
                  id={isSupporter ? "pages.support.subtitle_supporter" : "pages.support.subtitle"}
                  values={{
                    nhitomi: <span className="text-lg font-bold">nhitomi</span>,
                  }}
                />
              </div>

              <div className="text-gray-darker text-sm max-w-lg">
                {isSupporter ? <ThanksDescription /> : <SupportDescription />}
              </div>
            </div>
          </MainCard>
        ),
        [isSupporter]
      )}

      <div>
        {useMemo(
          () => (
            <Benefits />
          ),
          []
        )}

        <div className="divide-y divide-gray-darkest">
          <div className="h-16" />
          <div className="h-16" />
        </div>

        {useMemo(
          () => (
            <Checkout {...result} />
          ),
          [result]
        )}
      </div>
    </Container>
  );
};
