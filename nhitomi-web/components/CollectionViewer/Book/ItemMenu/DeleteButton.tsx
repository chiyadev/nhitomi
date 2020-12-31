import React, { memo, useState } from "react";
import { Book, BookContent, Collection } from "nhitomi-api";
import { Icon, Link, MenuItem, useToast } from "@chakra-ui/react";
import { useT } from "../../../../locales";
import ElementPortal from "../../../ElementPortal";
import { FaTrash } from "react-icons/fa";
import BlockingSpinner from "../../../BlockingSpinner";
import { createApiClient } from "../../../../utils/client";
import { useErrorToast } from "../../../../utils/hooks";
import { trackEvent } from "../../../../utils/umami";
import { captureException } from "@sentry/minimal";

const DeleteButton = ({ collection, book, content }: { collection: Collection; book: Book; content: BookContent }) => {
  const t = useT();
  const error = useErrorToast();
  const toast = useToast();
  const [load, setLoad] = useState(false);

  return (
    <>
      <BlockingSpinner visible={load} />

      <ElementPortal.Consumer>
        <MenuItem
          color="red.300"
          icon={<Icon as={FaTrash} />}
          onClick={async () => {
            setLoad(true);
            trackEvent("collectionViewer", "itemDelete");

            try {
              const client = createApiClient();

              await client.collection.removeCollectionItems({
                id: collection.id,
                collectionItemsRequest: {
                  items: [book.id],
                },
              });

              toast({
                title: t("CollectionViewer.Book.ItemMenu.DeleteButton.toastTitle"),
                description: t("CollectionViewer.Book.ItemMenu.DeleteButton.toastDescription", {
                  book: (
                    <Link href={`/books/${book.id}/contents/${content.id}`} color="blue.500" isExternal>
                      {book.primaryName}
                    </Link>
                  ),
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
            } catch (e) {
              captureException(e);
              error(e);
            } finally {
              setLoad(false);
            }
          }}
        >
          {t("CollectionViewer.Book.ItemMenu.DeleteButton.text")}
        </MenuItem>
      </ElementPortal.Consumer>
    </>
  );
};

export default memo(DeleteButton);
