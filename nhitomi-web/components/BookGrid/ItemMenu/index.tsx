import React, { Dispatch, memo, ReactNode, useContext, useMemo } from "react";
import { MenuDivider as MenuDividerCore } from "@chakra-ui/react";
import { Book, BookContent } from "nhitomi-api";
import DownloadButton from "./DownloadButton";
import FavoriteButton from "./FavoriteButton";
import AddToCollectionButton from "./AddToCollectionButton";
import { BookMenuContext } from "..";
import SourceButton from "./SourceButton";
import ElementPortal from "../../ElementPortal";

const MenuDivider = () => (
  <ElementPortal.Consumer>
    <MenuDividerCore />
  </ElementPortal.Consumer>
);

const ItemMenu = ({ book, content, setMenu }: { book: Book; content: BookContent; setMenu: Dispatch<ReactNode> }) => {
  const { render } = useContext(BookMenuContext);
  const additional = useMemo(() => render(book, content), [render, book, content]);

  return (
    <ElementPortal.Provider onRender={setMenu}>
      <FavoriteButton book={book} content={content} />
      <AddToCollectionButton book={book} content={content} />
      <MenuDivider />

      <SourceButton book={book} content={content} />
      <DownloadButton book={book} content={content} />

      {additional && (
        <>
          <MenuDivider />
          {additional}
        </>
      )}
    </ElementPortal.Provider>
  );
};

export default memo(ItemMenu);
