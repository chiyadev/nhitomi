import React, { memo } from "react";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../../utils/client";
import { selectBookContent } from "../../../utils/book";
import { CookieContainer, parseConfigs } from "../../../utils/config";
import ConfigProvider from "../../../components/ConfigProvider";
import { parseCookies } from "nookies";
import { parseQueries } from "../../../utils/query";
import ErrorPage from "../../../components/ErrorPage";
import { sanitizeProps } from "../../../utils/props";

type Props = {
  cookies: CookieContainer;
  result: {
    type: "error";
    message: string;
  };
};

export const getServerSideProps: GetServerSideProps<Props> = async (ctx) => {
  const cookies = parseCookies(ctx);
  const { token, searchLanguages, searchSources } = parseConfigs(cookies);
  const { id } = parseQueries(ctx.query);

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

    const book = await client.book.getBook({ id });
    const content = selectBookContent(book.contents, searchLanguages, searchSources);

    return {
      redirect: {
        destination: `/books/${book.id}/contents/${content.id}`,
        permanent: false,
      },
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

const BookRedirect = ({ cookies, result }: Props) => {
  return (
    <ConfigProvider cookies={cookies}>
      <ErrorPage {...result} />
    </ConfigProvider>
  );
};

export default memo(BookRedirect);
