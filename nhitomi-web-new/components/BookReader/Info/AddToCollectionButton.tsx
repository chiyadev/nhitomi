import React, { memo, useCallback, useState } from "react";
import { Button, Icon, Link, useToast } from "@chakra-ui/react";
import { FaPlus } from "react-icons/fa";
import { createApiClient, useClientInfoAuth } from "../../../utils/client";
import CollectionSelector from "../../CollectionSelector";
import { Book, Collection, CollectionInsertPosition, ObjectType } from "nhitomi-api";
import { useT } from "../../../locales";
import BlockingSpinner from "../../BlockingSpinner";
import { useErrorToast } from "../../../utils/hooks";

const AddToCollectionButton = ({ book }: { book: Book }) => {
  const t = useT();
  const info = useClientInfoAuth();
  const toast = useToast();
  const error = useErrorToast();
  const [select, setSelect] = useState(false);
  const [load, setLoad] = useState(false);

  const filter = useCallback(
    (collection: Collection) => collection.type === ObjectType.Book && !collection.items.includes(book.id),
    [book]
  );

  const add = useCallback(
    async (collection: Collection) => {
      setLoad(true);

      try {
        const client = createApiClient();

        if (client) {
          await client.collection.addCollectionItems({
            id: collection.id,
            addCollectionItemsRequest: {
              items: [book.id],
              position: CollectionInsertPosition.Start,
            },
          });

          toast({
            title: t("BookReader.Info.AddToCollectionButton.toastTitle"),
            description: t("BookReader.Info.AddToCollectionButton.toastDescription", {
              book: book.primaryName,
              collection: (
                <Link href={`/collections/${collection.id}`} color="blue.500" isExternal>
                  {collection.name}
                </Link>
              ),
            }),
            position: "top-right",
            status: "success",
            isClosable: true,
          });
        }
      } catch (e) {
        console.error(e);
        error(e);
      } finally {
        setLoad(false);
      }
    },
    [book, error]
  );

  return (
    <>
      {info && (
        <CollectionSelector open={select} setOpen={setSelect} userId={info.user.id} onFilter={filter} onSelect={add} />
      )}

      <BlockingSpinner visible={load} />

      <Button leftIcon={<Icon as={FaPlus} />} onClick={() => setSelect(true)}>
        {t("BookReader.Info.AddToCollectionButton.text")}
      </Button>
    </>
  );
};

export default memo(AddToCollectionButton);
