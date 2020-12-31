import React, { memo } from "react";
import { CookieContainer, parseConfigs } from "../../utils/config";
import { GetServerSideProps } from "next";
import { parseCookies } from "nookies";
import { createApiClient } from "../../utils/client";
import {
  GetInfoAuthenticatedResponse,
  GetInfoAuthenticatedResponseFromJSON,
  GetInfoAuthenticatedResponseToJSON,
  GetStripeInfoResponse,
  GetStripeInfoResponseFromJSON,
  GetStripeInfoResponseToJSON,
} from "nhitomi-api";
import { sanitizeProps } from "../../utils/props";
import { useChangeCount } from "../../utils/hooks";
import ConfigProvider from "../../components/ConfigProvider";
import ErrorPage from "../../components/ErrorPage";
import Content from "../../components/Support";

type Props = {
  cookies: CookieContainer;
  result:
    | {
        type: "success";
        info: GetInfoAuthenticatedResponse;
        stripe: GetStripeInfoResponse;
      }
    | {
        type: "error";
        message: string;
      };
};

export const getServerSideProps: GetServerSideProps<Props> = async (ctx) => {
  const cookies = parseCookies(ctx);
  const { token } = parseConfigs(cookies);

  try {
    if (!token) {
      return {
        redirect: {
          destination: "/auth",
          permanent: false,
        },
      };
    }

    const client = createApiClient(token);
    const info = await client.info.getInfoAuthenticated();
    const stripe = await client.info.getStripeInfo();

    return {
      props: sanitizeProps({
        cookies,
        result: {
          type: "success",
          info: GetInfoAuthenticatedResponseToJSON(info),
          stripe: GetStripeInfoResponseToJSON(stripe),
        },
      }),
    };
  } catch (e) {
    ctx.res.statusCode = 500;

    return {
      props: sanitizeProps({
        cookies,
        result: {
          type: "error",
          message: e.message,
        },
      }),
    };
  }
};

const Support = ({ cookies, result }: Props) => {
  const renderId = useChangeCount(result);

  switch (result.type) {
    case "success":
      return (
        <ConfigProvider key={renderId} cookies={cookies} info={GetInfoAuthenticatedResponseFromJSON(result.info)}>
          <Content stripe={GetStripeInfoResponseFromJSON(result.stripe)} />
        </ConfigProvider>
      );

    case "error":
      return (
        <ConfigProvider key={renderId} cookies={cookies}>
          <ErrorPage message={result.message} />
        </ConfigProvider>
      );
  }
};

export default memo(Support);
