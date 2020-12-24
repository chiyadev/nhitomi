import React, { memo, useMemo, useState } from "react";
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
import Layout from "../../../../components/Layout";
import ErrorPage from "../../../../components/ErrorPage";
import { CookieContainer, parseConfigs } from "../../../../utils/config";
import ConfigProvider from "../../../../components/ConfigProvider";
import Info from "../../../../components/BookReader/Info";
import Background from "../../../../components/BookReader/Background";
import { useChangeCount } from "../../../../utils/hooks";
import PageDisplay from "../../../../components/BookReader/PageDisplay";
import { parseCookies } from "nookies";
import { parseQueries } from "../../../../utils/query";
import { sanitizeProps } from "../../../../utils/props";
import LayoutBody from "../../../../components/LayoutBody";
import { ReaderScrollContext, ReaderScrollState } from "../../../../components/BookReader/scroll";
import ScrollKeyHandler from "../../../../components/ScrollKeyHandler";
import { useInView } from "react-intersection-observer";
import { VStack } from "@chakra-ui/layout";
import CursorManager from "../../../../components/BookReader/CursorManager";
import Header from "../../../../components/Header";
import HeaderTitle from "../../../../components/BookReader/HeaderTitle";

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

const Content = ({ book, contentId }: { book: Book; contentId: string }) => {
  const content = book.contents.find((content) => content.id === contentId) || book.contents[0];

  const [infoRef, infoVisible] = useInView();
  const [scroll, setScroll] = useState<ReaderScrollState>({ currentPage: 0, currentRow: 0 });

  return (
    <Layout title={[book.primaryName]}>
      <ReaderScrollContext.Provider value={useMemo(() => [scroll, setScroll], [scroll, setScroll])}>
        <ScrollKeyHandler />
        <Background book={book} content={content} visible={infoVisible} />

        <Header shadow title={<HeaderTitle book={book} content={content} />} />

        <VStack align="stretch" spacing={8}>
          <div ref={infoRef}>
            <LayoutBody>
              <Info book={book} content={content} />
            </LayoutBody>
          </div>

          <CursorManager>
            <PageDisplay book={book} content={content} />
          </CursorManager>
        </VStack>
      </ReaderScrollContext.Provider>
    </Layout>
  );
};

export default memo(BookReader);
