import React, { memo, useCallback, useRef, useState } from "react";
import { Book, BookSearchResult, ScraperType } from "nhitomi-api";
import { useT } from "../../locales";
import { useQuery } from "../../utils/query";
import { useConfig } from "../../utils/config";
import { QueryChunkSize } from "../../utils/constants";
import Layout from "../Layout";
import Header from "../Header";
import HeaderTitleQuery from "./HeaderTitleQuery";
import HeaderTitleApp from "./HeaderTitleApp";
import HeaderMenu from "./HeaderMenu";
import LayoutBody from "../LayoutBody";
import BookGrid from "../BookGrid";
import EmptyDisplay from "./EmptyDisplay";
import InfiniteLoader from "../BookGrid/InfiniteLoader";
import { createApiClient } from "../../utils/client";
import { createBookQuery } from "../../utils/book";
import TimingText from "./TimingText";

function removeDuplicates(books: Book[]) {
  const ids = new Set(books.map((book) => book.id));
  return books.filter((book) => ids.delete(book.id));
}

const BookListing = ({ initial }: { initial: BookSearchResult }) => {
  const t = useT();
  const [query] = useQuery("query");
  const [languages] = useConfig("searchLanguages");
  const [sort] = useQuery("sort");
  const [order] = useQuery("order");
  const [source] = useQuery("source");

  const [items, setItems] = useState<Book[]>(initial.items);
  const offset = useRef(QueryChunkSize);

  return (
    <Layout title={[query || t("BookListing.title")]}>
      <Header title={query ? <HeaderTitleQuery query={query} /> : <HeaderTitleApp />} menu={<HeaderMenu />} />

      <LayoutBody>
        <TimingText result={initial} />

        {items.length ? <BookGrid items={items} /> : <EmptyDisplay />}

        {QueryChunkSize < initial.total && (
          <InfiniteLoader
            hasMore={useCallback(async () => {
              const client = createApiClient();

              const { items: newItems, total } = await client.book.searchBooks({
                bookQuery: {
                  ...createBookQuery(query, languages, source.split(",") as ScraperType[], sort, order),
                  offset: offset.current,
                },
              });

              if (newItems.length) {
                setItems((items) => removeDuplicates([...items, ...newItems]));
                offset.current += newItems.length;

                return offset.current < total;
              }

              return false;
            }, [])}
          />
        )}
      </LayoutBody>
    </Layout>
  );
};

export default memo(BookListing);
