import { CookieContainer, parseConfigs } from "../../../utils/config";
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
  User,
  UserFromJSON,
  UserToJSON,
} from "nhitomi-api";
import { GetServerSideProps } from "next";
import { parseCookies } from "nookies";
import { createApiClient } from "../../../utils/client";
import { sanitizeProps } from "../../../utils/props";
import React, { memo } from "react";
import { useChangeCount } from "../../../utils/hooks";
import ConfigProvider from "../../../components/ConfigProvider";
import ErrorPage from "../../../components/ErrorPage";
import { parseQueries } from "../../../utils/query";
import Content from "../../../components/CollectionListing";

type Props = {
  cookies: CookieContainer;
  result:
    | {
        type: "success";
        info: GetInfoAuthenticatedResponse;
        user: User;
        books: {
          cover: Book | null;
          collection: Collection;
        }[];
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
    const user = info.user.id === id ? info.user : await client.user.getUser({ id });
    const { items } = await client.user.getUserCollections({ id });

    const books = items.filter((c) => c.type === ObjectType.Book);
    const bookIds = books.filter((c) => c.items.length).map((c) => c.items[0]);
    const bookCovers = bookIds.length ? await client.book.getBooks({ getBookManyRequest: { ids: bookIds } }) : [];

    return {
      props: sanitizeProps({
        cookies,
        result: {
          type: "success",
          info: GetInfoAuthenticatedResponseToJSON(info),
          user: UserToJSON(user),
          books: books.map((c) => ({
            cover: BookToJSON(bookCovers.find((book) => book.id === c.items[0]) || null),
            collection: CollectionToJSON(c),
          })),
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

const CollectionListing = ({ cookies, result }: Props) => {
  const renderId = useChangeCount(result);

  switch (result.type) {
    case "success":
      return (
        <ConfigProvider key={renderId} cookies={cookies} info={GetInfoAuthenticatedResponseFromJSON(result.info)}>
          <Content
            user={UserFromJSON(result.user)}
            books={result.books.map(({ cover, collection }) => ({
              cover: BookFromJSON(cover),
              collection: CollectionFromJSON(collection),
            }))}
          />
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

export default memo(CollectionListing);
