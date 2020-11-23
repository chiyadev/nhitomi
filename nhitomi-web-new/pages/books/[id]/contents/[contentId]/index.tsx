import React, { memo, useMemo } from "react";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../../../../utils/client";
import { sanitizeProps } from "../../../../../utils/props";
import { BookFromJSON, BookToJSON } from "nhitomi-api";
import Layout from "../../../../../components/Layout";
import ErrorPage from "../../../../../components/ErrorPage";
import { createRawConfig, RawConfig } from "../../../../../utils/config";
import ConfigProvider from "../../../../../components/ConfigProvider";
import Info from "../../../../../components/BookReader/Info";
import Background from "../../../../../components/BookReader/Background";
import { useChangeCount } from "../../../../../utils/hooks";

type Props = {
  config: RawConfig;
  data?: {
    book: any;
    contentId: string;
  };
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
    const { id, contentId } = ctx.query;
    const book = await client.book.getBook({
      id: Array.isArray(id) ? id[0] : id,
    });

    const content = book.contents.find((content) => content.id === contentId);

    if (!content) {
      throw Error(`Book ${book.id}/${contentId} not found.`);
    }

    return {
      props: sanitizeProps({
        config,
        data: {
          book: BookToJSON(book),
          contentId: content.id,
        },
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

const BookReader = ({ config, data, error }: Props) => {
  const contentId = useChangeCount(data);

  return (
    <ConfigProvider config={config}>
      {error ? <ErrorPage error={error} /> : <Content key={contentId} config={config} data={data} />}
    </ConfigProvider>
  );
};

const Content = ({ data }: Props) => {
  const book = useMemo(() => BookFromJSON(data?.book), [data]);
  const content = useMemo(() => book.contents.find((content) => content.id === data?.contentId) || book.contents[0], [
    book,
    data,
  ]);

  return (
    <Layout title={[book.primaryName]}>
      <Background book={book} content={content} />
      <Info book={book} content={content} />
    </Layout>
  );
};

export default memo(BookReader);
