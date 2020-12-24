import React, { memo, useCallback, useRef, useState } from "react";
import Layout from "../components/Layout";
import { GetServerSideProps } from "next";
import { createApiClient } from "../utils/client";
import {
  Book,
  BookSearchResult,
  BookSearchResultFromJSON,
  BookSearchResultToJSON,
  GetInfoAuthenticatedResponse,
  GetInfoAuthenticatedResponseFromJSON,
  GetInfoAuthenticatedResponseToJSON,
  ScraperType,
} from "nhitomi-api";
import ErrorPage from "../components/ErrorPage";
import { parseQueries, useQuery } from "../utils/query";
import Header from "../components/Header";
import InfiniteLoader from "../components/BookGrid/InfiniteLoader";
import { createBookQuery } from "../utils/book";
import ConfigProvider from "../components/ConfigProvider";
import { useChangeCount } from "../utils/hooks";
import BookGrid from "../components/BookGrid";
import { CookieContainer, parseConfigs, useConfig } from "../utils/config";
import { parseCookies } from "nookies";
import { sanitizeProps } from "../utils/props";
import LayoutBody from "../components/LayoutBody";
import HeaderTitleQuery from "../components/BookListing/HeaderTitleQuery";
import HeaderTitleApp from "../components/BookListing/HeaderTitleApp";
import HeaderMenu from "../components/BookListing/HeaderMenu";
import { useT } from "../locales";
import { QueryChunkSize } from "../utils/constants";

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

function removeDuplicates(books: Book[]) {
  const ids = new Set(books.map((book) => book.id));
  return books.filter((book) => ids.delete(book.id));
}

const Content = ({ initial }: { initial: BookSearchResult }) => {
  const t = useT();
  const [query] = useQuery("query");
  const [languages] = useConfig("searchLanguages");
  const [sort] = useQuery("sort");
  const [order] = useQuery("order");
  const [source] = useQuery("source");

  const [items, setItems] = useState<Book[]>(initial.items);
  const offset = useRef(QueryChunkSize);

  return (
    <Layout title={[query || t("BookListing.title")]}>
      <Header title={query ? <HeaderTitleQuery query={query} /> : <HeaderTitleApp />} menu={<HeaderMenu />} />

      <LayoutBody>
        <BookGrid items={items} />

        {QueryChunkSize < initial.total && (
          <InfiniteLoader
            hasMore={useCallback(async () => {
              const client = createApiClient();

              if (client) {
                const { items: newItems, total } = await client.book.searchBooks({
                  bookQuery: {
                    ...createBookQuery(query, languages, source.split(",") as ScraperType[], sort, order),
                    offset: offset.current,
                  },
                });

                if (newItems.length) {
                  setItems((items) => removeDuplicates([...items, ...newItems]));
                  offset.current += newItems.length;

                  return offset.current < total;
                }
              }

              return false;
            }, [])}
          />
        )}
      </LayoutBody>
    </Layout>
  );
};

export default memo(BookListing);
