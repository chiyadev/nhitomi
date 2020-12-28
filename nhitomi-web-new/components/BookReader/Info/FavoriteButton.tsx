import React, { memo, useState } from "react";
import { Button, Icon, LightMode, Link, useToast } from "@chakra-ui/react";
import { FaHeart } from "react-icons/fa";
import { useT } from "../../../locales";
import { Book, BookContent, ObjectType, SpecialCollection } from "nhitomi-api";
import { ClientUtils, createApiClient, useClientInfoAuth } from "../../../utils/client";
import { useErrorToast } from "../../../utils/hooks";

const FavoriteButton = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();
  const info = useClientInfoAuth();
  const toast = useToast();
  const error = useErrorToast();
  const [load, setLoad] = useState(false);

  return (
    <LightMode>
      <Button
        colorScheme="red"
        leftIcon={<Icon as={FaHeart} />}
        isLoading={load}
        onClick={async () => {
          setLoad(true);

          try {
            const client = createApiClient();

            if (client && info?.user) {
              const collection = await ClientUtils.addToSpecialCollection(
                client,
                info.user,
                ObjectType.Book,
                SpecialCollection.Favorites,
                book
              );

              toast({
                title: t("BookReader.Info.FavoriteButton.toastTitle"),
                description: t("BookReader.Info.FavoriteButton.toastDescription", {
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
        {t("BookReader.Info.FavoriteButton.text")}
      </Button>
    </LightMode>
  );
};

export default memo(FavoriteButton);
