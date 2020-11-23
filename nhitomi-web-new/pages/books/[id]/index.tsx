import React, { memo } from "react";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../../utils/client";
import { createSelectBookContentOptions, selectBookContent } from "../../../utils/book";
import { sanitizeProps } from "../../../utils/props";
import { createRawConfig, RawConfig } from "../../../utils/config";
import ConfigProvider from "../../../components/ConfigProvider";

type Props = {
  config: RawConfig;
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
    const { id } = ctx.query;
    const book = await client.book.getBook({
      id: Array.isArray(id) ? id[0] : id,
    });

    const content = selectBookContent(book.contents, createSelectBookContentOptions(config));

    return {
      redirect: {
        destination: `/books/${book.id}/contents/${content.id}`,
        permanent: false,
      },
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

const BookRedirect = ({ config }: Props) => {
  return <ConfigProvider config={config}></ConfigProvider>;
};

export default memo(BookRedirect);
