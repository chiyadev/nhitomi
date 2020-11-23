import React, { memo, useMemo, useRef, useState } from "react";
import Layout from "../../components/Layout";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../utils/client";
import { Book, BookSearchResultFromJSON, BookSearchResultToJSON } from "nhitomi-api";
import { sanitizeProps } from "../../utils/props";
import ErrorDisplay from "../../components/BookSearch/ErrorDisplay";
import { useQueryBoolean, useQueryString } from "../../utils/query";
import Header from "../../components/BookSearch/Header";
import Search from "../../components/BookSearch/Search";
import Router from "next/router";
import Item from "../../components/BookGrid/Item";
import { Flex, Grid } from "@chakra-ui/react";
import styles from "../../components/BookGrid/Grid.module.css";
import InfiniteLoader from "../../components/BookGrid/InfiniteLoader";
import { createBookQuery, createBookQueryOptions } from "../../utils/book";
import { createRawConfig, RawConfig } from "../../utils/config";
import ConfigProvider from "../../components/ConfigProvider";
import { useChangeCount } from "../../utils/hooks";

type Props = {
  config: RawConfig;
  initial?: any;
  error?: Error;
};

export const getServerSideProps: GetServerSideProps<Props> = async (ctx) => {
  const config = createRawConfig(ctx);
  const client = createApiClient(ctx);

  if (!client) {
    return {
      redirect: {
        destination: "/auth",
        permanent: false,
      },
    };
  }

  try {
    const result = await client.book.searchBooks({
      bookQuery: createBookQuery(createBookQueryOptions(config)),
    });

    return {
      props: sanitizeProps({
        config,
        initial: BookSearchResultToJSON(result),
      }),
    };
  } catch (e) {
    ctx.res.statusCode = 400;

    return {
      props: sanitizeProps({
        config,
        error: e,
      }),
    };
  }
};

const Books = ({ config, initial, error }: Props) => {
  const contentId = useChangeCount(initial);
  const [query] = useQueryString("query");
  const [search, setSearch] = useQueryBoolean("search");

  return (
    <ConfigProvider config={config}>
      <Layout title="nhitomi">
        <Header onSearch={() => setSearch(true)} />

        <Search
          value={query || ""}
          setValue={async (value) => {
            await Router.push({
              query: {
                ...Router.query,
                query: value || [],
                search: [],
              },
            });
          }}
          open={search}
          setOpen={setSearch}
        />

        {error ? <ErrorDisplay error={error} /> : <Content key={contentId} config={config} initial={initial} />}
      </Layout>
    </ConfigProvider>
  );
};

const Content = ({ initial, config }: Props) => {
  const [items, setItems] = useState<Book[]>(() => BookSearchResultFromJSON(initial).items);
  const offset = useRef(items.length);

  return (
    <>
      <Flex direction="column">
        <Grid p={2} gap={2} className={styles.grid}>
          {useMemo(() => items.map((book) => <Item key={book.id} book={book} />), [items])}
        </Grid>

        <InfiniteLoader
          hasMore={async () => {
            const client = createApiClient();

            if (client) {
              const { items: newItems } = await client.book.searchBooks({
                bookQuery: {
                  ...createBookQuery(createBookQueryOptions(config)),
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
          }}
        />
      </Flex>
    </>
  );
};

export default memo(Books);
