import React, { memo, useCallback, useRef, useState } from "react";
import { Book, Collection } from "nhitomi-api";
import { QueryChunkSize } from "../../../utils/constants";
import Layout from "../../Layout";
import Header from "../../Header";
import HeaderTitle from "../HeaderTitle";
import LayoutBody from "../../LayoutBody";
import BookGrid from "../../BookGrid";
import BookEmptyDisplay from "./EmptyDisplay";
import InfiniteLoader from "../../BookGrid/InfiniteLoader";
import { createApiClient } from "../../../utils/client";
import ItemMenu from "./ItemMenu";
import HeaderMenu from "./HeaderMenu";

function removeDuplicates<T extends { id: string }>(items: T[]) {
  const ids = new Set(items.map((book) => book.id));
  return items.filter((book) => ids.delete(book.id));
}

const CollectionViewer = ({ collection, initial }: { collection: Collection; initial: Book[] }) => {
  const [items, setItems] = useState(initial);
  const offset = useRef(QueryChunkSize);

  return (
    <Layout title={[collection.name]}>
      <Header back title={<HeaderTitle collection={collection} />} menu={<HeaderMenu collection={collection} />} />

      <LayoutBody>
        {items.length ? (
          <ItemMenu collection={collection}>
            <BookGrid items={items} />
          </ItemMenu>
        ) : (
          <BookEmptyDisplay />
        )}

        {QueryChunkSize < collection.items.length && (
          <InfiniteLoader
            hasMore={useCallback(async () => {
              const client = createApiClient();

              const newItems = await client.book.getBooks({
                getBookManyRequest: {
                  ids: collection.items.slice(offset.current, offset.current + QueryChunkSize),
                },
              });

              if (newItems.length) {
                setItems((items) => removeDuplicates([...items, ...newItems]));
                offset.current += QueryChunkSize;

                return offset.current < collection.items.length;
              }

              return false;
            }, [])}
          />
        )}
      </LayoutBody>
    </Layout>
  );
};

export default memo(CollectionViewer);
