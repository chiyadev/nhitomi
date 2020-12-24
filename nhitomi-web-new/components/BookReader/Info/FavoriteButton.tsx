import React, { memo, useState } from "react";
import { Button, Icon, LightMode, Link, useToast } from "@chakra-ui/react";
import { FaHeart } from "react-icons/fa";
import { useT } from "../../../locales";
import { Book, Collection, CollectionInsertPosition, ObjectType, SpecialCollection } from "nhitomi-api";
import { createApiClient, useClientInfoAuth } from "../../../utils/client";
import { useErrorToast } from "../../../utils/hooks";

const FavoriteButton = ({ book }: { book: Book }) => {
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
              let collection: Collection;
              let collectionId = info.user.specialCollections?.book?.favorites;

              for (let i = 0; ; i++) {
                try {
                  if (collectionId) {
                    collection = await client.collection.addCollectionItems({
                      id: collectionId,
                      addCollectionItemsRequest: {
                        items: [book.id],
                        position: CollectionInsertPosition.Start,
                      },
                    });

                    break;
                  }
                } catch (e) {
                  if (i === 1) {
                    throw e;
                  }
                }

                collectionId = (
                  await client.user.getUserSpecialCollection({
                    id: info.user.id,
                    type: ObjectType.Book,
                    collection: SpecialCollection.Favorites,
                  })
                ).id;
              }

              toast({
                title: t("BookReader.Info.FavoriteButton.toastTitle"),
                description: t("BookReader.Info.FavoriteButton.toastDescription", {
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
        }}
      >
        {t("BookReader.Info.FavoriteButton.text")}
      </Button>
    </LightMode>
  );
};

export default memo(FavoriteButton);
