import React, { memo } from "react";
import { GetServerSideProps } from "next";
import { createApiClient } from "../utils/client";
import {
  BookSearchResult,
  BookSearchResultFromJSON,
  BookSearchResultToJSON,
  GetInfoAuthenticatedResponse,
  GetInfoAuthenticatedResponseFromJSON,
  GetInfoAuthenticatedResponseToJSON,
  ScraperType,
} from "nhitomi-api";
import ErrorPage from "../components/ErrorPage";
import { parseQueries } from "../utils/query";
import { createBookQuery } from "../utils/book";
import ConfigProvider from "../components/ConfigProvider";
import { useChangeCount } from "../utils/hooks";
import { CookieContainer, parseConfigs } from "../utils/config";
import { parseCookies } from "nookies";
import { sanitizeProps } from "../utils/props";
import Content from "../components/BookListing";

type Props = {
  cookies: CookieContainer;
  result:
    | {
        type: "success";
        info: GetInfoAuthenticatedResponse;
        initial: BookSearchResult;
      }
    | {
        type: "error";
        message: string;
      };
};

export const getServerSideProps: GetServerSideProps<Props> = async (ctx) => {
  const cookies = parseCookies(ctx);
  const { token, searchLanguages } = parseConfigs(cookies);
  const { query, sort, order, source } = parseQueries(ctx.query);

  try {
    const client = createApiClient(token);

    if (!client) {
      return {
        redirect: {
          destination: "/auth",
          permanent: false,
        },
      };
    }

    const info = await client.info.getInfoAuthenticated();

    const result = await client.book.searchBooks({
      bookQuery: createBookQuery(query, searchLanguages, source.split(",") as ScraperType[], sort, order),
    });

    return {
      props: sanitizeProps({
        cookies,
        result: {
          type: "success",
          info: GetInfoAuthenticatedResponseToJSON(info),
          initial: BookSearchResultToJSON(result),
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

const BookListing = ({ cookies, result }: Props) => {
  const renderId = useChangeCount(result);

  switch (result.type) {
    case "success":
      return (
        <ConfigProvider key={renderId} cookies={cookies} info={GetInfoAuthenticatedResponseFromJSON(result.info)}>
          <Content initial={BookSearchResultFromJSON(result.initial)} />
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

export default memo(BookListing);
