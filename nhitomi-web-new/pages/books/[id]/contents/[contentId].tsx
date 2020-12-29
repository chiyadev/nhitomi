import React, { memo } from "react";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../../../utils/client";
import {
  Book,
  BookFromJSON,
  BookToJSON,
  GetInfoAuthenticatedResponse,
  GetInfoAuthenticatedResponseFromJSON,
  GetInfoAuthenticatedResponseToJSON,
} from "nhitomi-api";
import ErrorPage from "../../../../components/ErrorPage";
import { CookieContainer, parseConfigs } from "../../../../utils/config";
import ConfigProvider from "../../../../components/ConfigProvider";
import { useChangeCount } from "../../../../utils/hooks";
import { parseCookies } from "nookies";
import { parseQueries } from "../../../../utils/query";
import { sanitizeProps } from "../../../../utils/props";
import Content from "../../../../components/BookReader";

type Props = {
  cookies: CookieContainer;
  result:
    | {
        type: "success";
        id: string;
        contentId: string;
        info: GetInfoAuthenticatedResponse;
        book: Book;
      }
    | {
        type: "error";
        message: string;
      };
};

export const getServerSideProps: GetServerSideProps<Props> = async (ctx) => {
  const cookies = parseCookies(ctx);
  const { token } = parseConfigs(cookies);
  const { id, contentId } = parseQueries(ctx.query);

  try {
    if (!token) {
      return {
        redirect: {
          destination: "/auth",
          permanent: false,
        },
      };
    }

    const client = createApiClient(token);

    const info = await client.info.getInfoAuthenticated();
    const book = await client.book.getBook({ id });
    const content = book.contents.find((content) => content.id === contentId);

    if (!content) {
      throw Error(`Book '${id}/${contentId}' not found.`);
    }

    return {
      props: sanitizeProps({
        cookies,
        result: {
          type: "success",
          id,
          contentId,
          info: GetInfoAuthenticatedResponseToJSON(info),
          book: BookToJSON(book),
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

const BookReader = ({ cookies, result }: Props) => {
  const renderId = useChangeCount(result);

  switch (result.type) {
    case "success":
      return (
        <ConfigProvider key={renderId} cookies={cookies} info={GetInfoAuthenticatedResponseFromJSON(result.info)}>
          <Content book={BookFromJSON(result.book)} contentId={result.contentId} />
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

export default memo(BookReader);
