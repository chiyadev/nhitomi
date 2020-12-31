import React, { memo, ReactNode, useCallback, useMemo } from "react";
import { BookMenuContext } from "../../../BookGrid";
import { Book, BookContent, Collection } from "nhitomi-api";
import DeleteButton from "./DeleteButton";

const ItemMenu = ({ collection, children }: { collection: Collection; children?: ReactNode }) => {
  const render = useCallback(
    (book: Book, content: BookContent) => {
      return (
        <>
          <DeleteButton collection={collection} book={book} content={content} />
        </>
      );
    },
    [collection]
  );

  return <BookMenuContext.Provider value={useMemo(() => ({ render }), [render])}>{children}</BookMenuContext.Provider>;
};

export default memo(ItemMenu);
