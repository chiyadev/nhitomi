import React, { memo } from "react";
import { CookieContainer, parseConfigs } from "../utils/config";
import { GetInfoResponse, GetInfoResponseFromJSON, GetInfoResponseToJSON } from "nhitomi-api";
import { GetServerSideProps } from "next";
import { parseCookies } from "nookies";
import { createApiClient } from "../utils/client";
import { sanitizeProps } from "../utils/props";
import { useChangeCount } from "../utils/hooks";
import ConfigProvider from "../components/ConfigProvider";
import Content from "../components/Auth";
import ErrorPage from "../components/ErrorPage";

type Props = {
  cookies: CookieContainer;
  result:
    | {
        type: "success";
        info: GetInfoResponse;
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
    if (token) {
      return {
        redirect: {
          destination: "/",
          permanent: false,
        },
      };
    }

    const client = createApiClient(token);
    const info = await client.info.getInfo();

    return {
      props: sanitizeProps({
        cookies,
        result: {
          type: "success",
          info: GetInfoResponseToJSON(info),
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

const Auth = ({ cookies, result }: Props) => {
  const renderId = useChangeCount(result);

  switch (result.type) {
    case "success":
      return (
        <ConfigProvider key={renderId} cookies={cookies} info={GetInfoResponseFromJSON(result.info)}>
          <Content />
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

export default memo(Auth);
