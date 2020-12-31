import React, { memo, useState } from "react";
import { Button, Icon, Link } from "@chakra-ui/react";
import { FaPlus } from "react-icons/fa";
import { useClientInfoAuth } from "../../../utils/client";
import { Book, BookContent, ObjectType } from "nhitomi-api";
import { useT } from "../../../locales";
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

      <Button
        leftIcon={<Icon as={FaPlus} />}
        onClick={() => {
          setAdd(true);
          trackEvent("bookReader", "addToCollection");
        }}
      >
        {t("BookReader.Info.AddToCollectionButton.text")}
      </Button>
    </>
  );
};

export default memo(AddToCollectionButton);
