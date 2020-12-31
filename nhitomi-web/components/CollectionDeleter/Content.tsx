import React, { Dispatch, memo, Ref, useState } from "react";
import {
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  Button,
  ButtonGroup,
  Icon,
  Link,
} from "@chakra-ui/react";
import { FaTrash } from "react-icons/fa";
import { useT } from "../../locales";
import { Collection } from "nhitomi-api";
import { useErrorToast } from "../../utils/hooks";
import { createApiClient } from "../../utils/client";
import { trackEvent } from "../../utils/umami";
import { captureException } from "@sentry/minimal";

const Content = ({
  cancelRef,
  setOpen,
  collection,
  onDelete,
}: {
  cancelRef: Ref<HTMLButtonElement>;
  setOpen: Dispatch<boolean>;
  collection: Collection;
  onDelete?: () => void;
}) => {
  const t = useT();
  const error = useErrorToast();
  const [load, setLoad] = useState(false);

  return (
    <>
      <AlertDialogHeader>{t("CollectionDeleter.title")}</AlertDialogHeader>

      <AlertDialogBody>
        {t("CollectionDeleter.description", {
          collection: (
            <Link href={`/collections/${collection.id}`} color="blue.300" isExternal>
              {collection.name}
            </Link>
          ),
        })}
      </AlertDialogBody>

      <AlertDialogFooter>
        <ButtonGroup>
          <Button
            colorScheme="red"
            leftIcon={<Icon as={FaTrash} />}
            isLoading={load}
            onClick={async () => {
              setLoad(true);
              trackEvent("collectionDeleter", `delete${collection.type}`);

              try {
                const client = createApiClient();

                await client.collection.deleteCollection({
                  id: collection.id,
                });

                onDelete?.();
              } catch (e) {
                captureException(e);
                error(e);
              } finally {
                setLoad(false);
              }
            }}
          >
            {t("CollectionDeleter.delete")}
          </Button>

          <Button ref={cancelRef} onClick={() => setOpen(false)} disabled={load}>
            {t("CollectionDeleter.cancel")}
          </Button>
        </ButtonGroup>
      </AlertDialogFooter>
    </>
  );
};

export default memo(Content);
