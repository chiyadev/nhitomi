import React, { memo, useState } from "react";
import { useT } from "../../../locales";
import { Icon, Link, MenuItem, useToast } from "@chakra-ui/react";
import { FaHeart } from "react-icons/fa";
import { Book, BookContent, ObjectType, SpecialCollection } from "nhitomi-api";
import { ClientUtils, createApiClient, useClientInfoAuth } from "../../../utils/client";
import { useErrorToast } from "../../../utils/hooks";
import ElementPortal from "../../ElementPortal";
import BlockingSpinner from "../../BlockingSpinner";

const FavoriteButton = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();
  const info = useClientInfoAuth();
  const toast = useToast();
  const error = useErrorToast();
  const [load, setLoad] = useState(false);

  return (
    <>
      <BlockingSpinner visible={load} />

      <ElementPortal.Consumer>
        <MenuItem
          icon={<Icon as={FaHeart} color="red.300" />}
          onClick={async () => {
            setLoad(true);

            try {
              if (info?.user) {
                const client = createApiClient();

                const collection = await ClientUtils.addToSpecialCollection(
                  client,
                  info.user,
                  ObjectType.Book,
                  SpecialCollection.Favorites,
                  book
                );

                toast({
                  title: t("BookGrid.ItemMenu.FavoriteButton.toastTitle"),
                  description: t("BookGrid.ItemMenu.FavoriteButton.toastDescription", {
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
              }
            } catch (e) {
              console.error(e);
              error(e);
            } finally {
              setLoad(false);
            }
          }}
        >
          {t("BookGrid.ItemMenu.FavoriteButton.text")}
        </MenuItem>
      </ElementPortal.Consumer>
    </>
  );
};

export default memo(FavoriteButton);
