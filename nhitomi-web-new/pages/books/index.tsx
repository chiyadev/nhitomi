import React, { memo, useCallback, useRef, useState } from "react";
import Layout from "../../components/Layout";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../utils/client";
import { Book, BookSearchResultFromJSON, BookSearchResultToJSON } from "nhitomi-api";
import ErrorPage from "../../components/ErrorPage";
import { parseQueries, useQuery } from "../../utils/query";
import Header from "../../components/BookSearch/Header";
import Search from "../../components/BookSearch/Search";
import { Flex } from "@chakra-ui/react";
import InfiniteLoader from "../../components/BookGrid/InfiniteLoader";
import { createBookQuery } from "../../utils/book";
import ConfigProvider from "../../components/ConfigProvider";
import { useChangeCount } from "../../utils/hooks";
import BookGrid from "../../components/BookGrid";
import { CookieContainer, parseConfigs, useConfig } from "../../utils/config";
import { parseCookies } from "nookies";
import { sanitizeProps } from "../../utils/props";
import LayoutBody from "../../components/LayoutBody";

type Props = {
  cookies: CookieContainer;
  result:
    | {
        type: "success";
        initial: any;
      }
    | {
        type: "error";
        message: string;
      };
};

export const getServerSideProps: GetServerSideProps<Props> = async (ctx) => {
  const cookies = parseCookies(ctx);
  const { token, searchLanguages, searchSources } = parseConfigs(cookies);
  const { query, sort, order } = parseQueries(ctx.query);

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

    const result = await client.book.searchBooks({
      bookQuery: createBookQuery(query, searchLanguages, searchSources, sort, order),
    });

    return {
      props: sanitizeProps({
        cookies,
        result: {
          type: "success",
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

const Books = ({ cookies, result }: Props) => {
  const renderId = useChangeCount(result);

  switch (result.type) {
    case "success":
      return (
        <ConfigProvider key={renderId} cookies={cookies}>
          <Content {...result} />
        </ConfigProvider>
      );

    case "error":
      return (
        <ConfigProvider cookies={cookies}>
          <ErrorPage {...result} />
        </ConfigProvider>
      );
  }
};

const Content = ({ initial }: { initial: any }) => {
  const [query, setQuery] = useQuery("query");
  const [languages] = useConfig("searchLanguages");
  const [sources] = useConfig("searchSources");
  const [sort] = useQuery("sort");
  const [order] = useQuery("order");

  const [items, setItems] = useState<Book[]>(() => BookSearchResultFromJSON(initial).items);
  const offset = useRef(items.length);

  const [search, setSearch] = useState(false);

  return (
    <Layout title={[query]}>
      <LayoutBody>
        <Header onSearch={useCallback(() => setSearch(true), [])} />

        <Search
          value={query}
          setValue={useCallback(async (value) => {
            await setQuery(value, "push");
          }, [])}
          open={search}
          setOpen={setSearch}
        />

        <Flex direction="column">
          <BookGrid items={items} />

          <InfiniteLoader
            hasMore={useCallback(async () => {
              const client = createApiClient();

              if (client) {
                const { items: newItems } = await client.book.searchBooks({
                  bookQuery: {
                    ...createBookQuery(query, languages, sources, sort, order),
                    offset: offset.current,
                  },
                });

                if (newItems.length) {
                  setItems((items) => [...items, ...newItems]);
                  offset.current += newItems.length;

                  return true;
                }
              }

              return false;
            }, [])}
          />
        </Flex>
      </LayoutBody>
    </Layout>
  );
};

export default memo(Books);
