import React, { memo, useMemo, useRef, useState } from "react";
import Layout from "../../components/Layout";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../utils/client";
import { Book, BookSearchResultFromJSON, BookSearchResultToJSON } from "nhitomi-api";
import { sanitizeProps } from "../../utils/props";
import { createQuery } from "../../components/BookSearch/query";
import { parseCookies } from "nookies";
import ErrorDisplay from "../../components/BookSearch/ErrorDisplay";
import { useQueryBoolean, useQueryString } from "../../utils/query";
import Header from "../../components/BookSearch/Header";
import Search from "../../components/BookSearch/Search";
import Router from "next/router";
import Item from "../../components/BookGrid/Item";
import { Flex, Grid } from "@chakra-ui/react";
import styles from "../../components/BookGrid/Grid.module.css";
import InfiniteLoader from "../../components/BookGrid/InfiniteLoader";

type Props = {
  initial?: any;
  error?: Error;
};

export const getServerSideProps: GetServerSideProps<Props> = async ({ req, res, query }) => {
  try {
    const client = createApiClient(req);

    if (!client) {
      return {
        redirect: {
          destination: "/auth",
          permanent: false,
          statusCode: 401,
        },
      };
    }

    const result = await client.book.searchBooks({
      bookQuery: createQuery({
        ...parseCookies({ req }),
        ...query,
      }),
    });

    return {
      props: sanitizeProps({
        initial: BookSearchResultToJSON(result),
      }),
    };
  } catch (e) {
    res.statusCode = 400;

    return {
      props: sanitizeProps({
        error: e,
      }),
    };
  }
};

const Books = ({ initial, error }: Props) => {
  const [query] = useQueryString("query");
  const [search, setSearch] = useQueryBoolean("search");

  const pageId = useRef(0);
  useMemo(() => pageId.current++, [initial]);

  return (
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

      {error ? <ErrorDisplay error={error} /> : <Content key={pageId.current} initial={initial} />}
    </Layout>
  );
};

const Content = ({ initial }: Props) => {
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
                  ...createQuery({
                    ...parseCookies(),
                    ...Router.query,
                  }),
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
