import React, { Dispatch, memo, useRef } from "react";
import { AlertDialog, AlertDialogContent, AlertDialogOverlay } from "@chakra-ui/react";
import { Collection } from "nhitomi-api";
import Content from "./Content";

const CollectionDeleter = ({
  open,
  setOpen,
  collection,
  onDelete,
}: {
  open: boolean;
  setOpen: Dispatch<boolean>;
  collection: Collection;
  onDelete?: () => void;
}) => {
  const cancelRef = useRef<HTMLButtonElement>(null);

  return (
    <AlertDialog isOpen={open} isCentered onClose={() => setOpen(false)} leastDestructiveRef={cancelRef}>
      <AlertDialogOverlay />
      <AlertDialogContent>
        <Content cancelRef={cancelRef} setOpen={setOpen} collection={collection} onDelete={onDelete} />
      </AlertDialogContent>
    </AlertDialog>
  );
};

export default memo(CollectionDeleter);
