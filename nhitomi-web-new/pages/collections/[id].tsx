import React, { memo } from "react";
import { GetServerSideProps } from "next";
import { createApiClient } from "../../utils/client";
import { CookieContainer, parseConfigs } from "../../utils/config";
import ConfigProvider from "../../components/ConfigProvider";
import { parseCookies } from "nookies";
import { parseQueries } from "../../utils/query";
import ErrorPage from "../../components/ErrorPage";
import { sanitizeProps } from "../../utils/props";
import { useChangeCount } from "../../utils/hooks";
import {
  Book,
  BookFromJSON,
  BookToJSON,
  Collection,
  CollectionFromJSON,
  CollectionToJSON,
  GetInfoAuthenticatedResponse,
  GetInfoAuthenticatedResponseFromJSON,
  GetInfoAuthenticatedResponseToJSON,
  ObjectType,
} from "nhitomi-api";
import { QueryChunkSize } from "../../utils/constants";
import ContentBook from "../../components/CollectionViewer/Book";

type Props = {
  cookies: CookieContainer;
  result:
    | {
        type: "success_book";
        info: GetInfoAuthenticatedResponse;
        collection: Collection;
        initial: Book[];
      }
    | {
        type: "error";
        message: string;
      };
};

export const getServerSideProps: GetServerSideProps<Props> = async (ctx) => {
  const cookies = parseCookies(ctx);
  const { token } = parseConfigs(cookies);
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

    const info = await client.info.getInfoAuthenticated();
    const collection = await client.collection.getCollection({ id });

    switch (collection.type) {
      case ObjectType.Book: {
        let books: Book[] = [];

        if (collection.items.length) {
          books = await client.book.getBooks({
            getBookManyRequest: {
              ids: collection.items.slice(0, QueryChunkSize),
            },
          });
        }

        return {
          props: sanitizeProps({
            cookies,
            result: {
              type: "success_book",
              info: GetInfoAuthenticatedResponseToJSON(info),
              collection: CollectionToJSON(collection),
              initial: books.map(BookToJSON),
            },
          }),
        };
      }

      default:
        throw Error(`Collection type '${collection.type}' is not supported.`);
    }
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

const CollectionViewer = ({ cookies, result }: Props) => {
  const renderId = useChangeCount(result);

  switch (result.type) {
    case "success_book":
      return (
        <ConfigProvider key={renderId} cookies={cookies} info={GetInfoAuthenticatedResponseFromJSON(result.info)}>
          <ContentBook collection={CollectionFromJSON(result.collection)} initial={result.initial.map(BookFromJSON)} />
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

export default memo(CollectionViewer);
