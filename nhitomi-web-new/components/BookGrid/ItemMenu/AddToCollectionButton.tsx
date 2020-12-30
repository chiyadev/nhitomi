import React, { memo, useState } from "react";
import { useT } from "../../../locales";
import { Icon, Link, MenuItem } from "@chakra-ui/react";
import { FaPlus } from "react-icons/fa";
import { Book, BookContent, ObjectType } from "nhitomi-api";
import { useClientInfoAuth } from "../../../utils/client";
import ElementPortal from "../../ElementPortal";
import CollectionItemAdder from "../../CollectionSelector/CollectionItemAdder";
import { trackEvent } from "../../../utils/umami";

const AddToCollectionButton = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();
  const info = useClientInfoAuth();
  const [add, setAdd] = useState(false);

  return (
    <>
      {info && (
        <CollectionItemAdder
          open={add}
          setOpen={setAdd}
          userId={info.user.id}
          itemId={book.id}
          itemName={
            <Link href={`/books/${book.id}/contents/${content.id}`} color="blue.500" isExternal>
              {book.primaryName}
            </Link>
          }
          itemType={ObjectType.Book}
        />
      )}

      <ElementPortal.Consumer>
        <MenuItem
          icon={<Icon as={FaPlus} />}
          onClick={() => {
            setAdd(true);
            trackEvent("bookGrid", "itemAddToCollection");
          }}
        >
          {t("BookGrid.ItemMenu.AddToCollectionButton.text")}
        </MenuItem>
      </ElementPortal.Consumer>
    </>
  );
};

export default memo(AddToCollectionButton);
