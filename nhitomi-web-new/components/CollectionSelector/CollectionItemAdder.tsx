import React, { Dispatch, memo, ReactNode, useCallback, useState } from "react";
import { Collection, CollectionInsertPosition, ObjectType } from "nhitomi-api";
import { createApiClient } from "../../utils/client";
import { Link, useToast } from "@chakra-ui/react";
import { useT } from "../../locales";
import { useErrorToast } from "../../utils/hooks";
import CollectionSelector from ".";
import BlockingSpinner from "../BlockingSpinner";
import { trackEvent } from "../../utils/umami";
import { captureException } from "@sentry/minimal";

const CollectionItemAdder = ({
  open,
  setOpen,
  userId,
  itemId,
  itemType,
  itemName,
}: {
  open: boolean;
  setOpen: Dispatch<boolean>;
  userId: string;
  itemId: string;
  itemType: ObjectType;
  itemName: ReactNode;
}) => {
  const t = useT();
  const toast = useToast();
  const error = useErrorToast();
  const [load, setLoad] = useState(false);

  const filter = useCallback(
    (collection: Collection) => collection.type === itemType && !collection.items.includes(itemId),
    [itemType, itemId]
  );

  const add = useCallback(
    async (collection: Collection) => {
      setLoad(true);
      trackEvent("collectionItemAdder", `add${itemType}`);

      try {
        const client = createApiClient();

        await client.collection.addCollectionItems({
          id: collection.id,
          addCollectionItemsRequest: {
            items: [itemId],
            position: CollectionInsertPosition.Start,
          },
        });

        toast({
          title: t("CollectionSelector.CollectionItemAdder.toastTitle"),
          description: t("CollectionSelector.CollectionItemAdder.toastDescription", {
            book: itemName,
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
    },
    [itemId, itemType, itemName, error]
  );

  return (
    <>
      <BlockingSpinner visible={load} />
      <CollectionSelector open={open} setOpen={setOpen} userId={userId} onFilter={filter} onSelect={add} />
    </>
  );
};

export default memo(CollectionItemAdder);
