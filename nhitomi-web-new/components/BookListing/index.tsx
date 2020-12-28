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
import { chakra } from "@chakra-ui/react";
import BookGrid from "../BookGrid";
import EmptyDisplay from "./EmptyDisplay";
import InfiniteLoader from "../BookGrid/InfiniteLoader";
import { createApiClient } from "../../utils/client";
import { createBookQuery } from "../../utils/book";

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
        <chakra.div px={2} color="gray.500" fontSize="sm">
          {t("BookListing.timing", {
            count: initial.total,
            time: Math.round(parseFloat(initial.took.split(":").slice(-1)[0]) * 100) / 100,
          })}
        </chakra.div>

        {items.length ? <BookGrid items={items} /> : <EmptyDisplay />}

        {QueryChunkSize < initial.total && (
          <InfiniteLoader
            hasMore={useCallback(async () => {
              const client = createApiClient();

              if (client) {
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
