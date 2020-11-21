import React, { memo, useMemo } from "react";
import Layout from "../../components/Layout";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../utils/client";
import { BookSearchResultFromJSON, BookSearchResultToJSON, BookSort, SortDirection } from "nhitomi-api";
import { SerializableError } from "../../utils/errors";
import { sanitizeProps } from "../../utils/props";

type Props = {
  initial: any;
  error?: SerializableError;
};

export const getServerSideProps: GetServerSideProps<Props> = async ({ req }) => {
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
    bookQuery: {
      sorting: [
        {
          value: BookSort.UpdatedTime,
          direction: SortDirection.Descending,
        },
      ],
      limit: 50,
    },
  });

  return {
    props: sanitizeProps({
      initial: BookSearchResultToJSON(result),
    }),
  };
};

const Books = ({ initial }: Props) => {
  const { items } = useMemo(() => BookSearchResultFromJSON(initial), [initial]);

  return (
    <Layout title="nhitomi">
      <h1>
        <span>Hello, new nhitomi!</span>
        {items.map((book) => (
          <div>{book.primaryName}</div>
        ))}
      </h1>
    </Layout>
  );
};

export default memo(Books);
