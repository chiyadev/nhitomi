import React, { memo, useMemo } from "react";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../../../../utils/client";
import { sanitizeProps } from "../../../../../utils/props";
import { BookFromJSON, BookToJSON } from "nhitomi-api";
import Layout from "../../../../../components/Layout";
import ErrorDisplay from "../../../../../components/BookSearch/ErrorDisplay";

type Props = {
  data?: {
    book: any;
    contentId: string;
  };
  error?: Error;
};

export const getServerSideProps: GetServerSideProps<Props> = async ({ req, res, query: { id, contentId } }) => {
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

    const book = await client.book.getBook({
      id: Array.isArray(id) ? id[0] : id,
    });

    const content = book.contents.find((content) => content.id === contentId);

    if (!content) {
      throw Error(`Book ${book.id}/${contentId} not found.`);
    }

    return {
      props: sanitizeProps({
        data: {
          book: BookToJSON(book),
          contentId: content.id,
        },
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

const BookReader = ({ data, error }: Props) => {
  return <Layout title="nhitomi">{error ? <ErrorDisplay error={error} /> : <Content data={data} />}</Layout>;
};

const Content = ({ data }: Props) => {
  const book = useMemo(() => BookFromJSON(data?.book), [data]);
  const content = useMemo(() => book.contents.find((content) => content.id === data?.contentId) || book.contents[0], [
    book,
    data,
  ]);

  return (
    <div>
      {book.primaryName} {book.id} {content.id}
    </div>
  );
};

export default memo(BookReader);
