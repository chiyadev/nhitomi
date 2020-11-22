import React, { memo, useMemo } from "react";
import Layout from "../../components/Layout";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../utils/client";
import { BookSearchResultFromJSON, BookSearchResultToJSON } from "nhitomi-api";
import { sanitizeProps } from "../../utils/props";
import { createQuery } from "../../components/BookSearch/query";
import { parseCookies } from "nookies";
import ErrorDisplay from "../../components/BookSearch/ErrorDisplay";
import { useQueryBoolean, useQueryString } from "../../utils/query";
import Header from "../../components/BookSearch/Header";
import Search from "../../components/BookSearch/Search";
import { useRouter } from "next/router";

type Props = {
  initial?: any;
  error?: Error;
};

export const getServerSideProps: GetServerSideProps<Props> = async ({ req, query }) => {
  try {
    const client = createApiClient(req);

    if (!client) {
      return {
        redirect: {
          destination: "/auth",
          permanent: false,
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
    return {
      props: sanitizeProps({
        error: e,
      }),
    };
  }
};

const Books = ({ initial, error }: Props) => {
  const router = useRouter();
  const [query] = useQueryString("query");
  const [search, setSearch] = useQueryBoolean("search");

  return (
    <Layout title="nhitomi">
      <Header onSearch={() => setSearch(true)} />

      <Search
        value={query || ""}
        setValue={async (value) => {
          await router.push({
            query: {
              ...router.query,
              query: value || [],
              search: [],
            },
          });
        }}
        open={search}
        setOpen={setSearch}
      />

      {error ? <ErrorDisplay error={error} /> : <Items initial={initial} />}
    </Layout>
  );
};

const Items = ({ initial }: Props) => {
  const { items } = useMemo(() => BookSearchResultFromJSON(initial), [initial]);

  return (
    <div>
      {items.map((book) => (
        <div>{book.primaryName}</div>
      ))}
    </div>
  );
};

export default memo(Books);
