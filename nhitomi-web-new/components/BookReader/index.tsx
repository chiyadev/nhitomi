import React, { memo, useMemo, useState } from "react";
import { Book } from "nhitomi-api";
import { useInView } from "react-intersection-observer";
import { ReaderScrollContext, ReaderScrollState } from "./scroll";
import Layout from "../Layout";
import ScrollKeyHandler from "../ScrollKeyHandler";
import Background from "./Background";
import Header from "../Header";
import HeaderTitle from "./HeaderTitle";
import { VStack } from "@chakra-ui/layout";
import LayoutBody from "../LayoutBody";
import Info from "./Info";
import CursorManager from "./CursorManager";
import PageDisplay from "./PageDisplay";

const BookReader = ({ book, contentId }: { book: Book; contentId: string }) => {
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
