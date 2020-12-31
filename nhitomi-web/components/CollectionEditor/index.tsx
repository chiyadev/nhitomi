import React, { Dispatch, memo, useRef } from "react";
import { Drawer, DrawerContent, DrawerOverlay } from "@chakra-ui/react";
import Content from "./Content";
import { Collection } from "nhitomi-api";

const CollectionEditor = ({
  open,
  setOpen,
  collection,
  onSubmit,
}: {
  open: boolean;
  setOpen: Dispatch<boolean>;
  collection: Collection;
  onSubmit?: Dispatch<Collection>;
}) => {
  const focusRef = useRef<HTMLInputElement>(null);

  return (
    <Drawer placement="right" size="md" isOpen={open} onClose={() => setOpen(false)} initialFocusRef={focusRef}>
      <DrawerOverlay />
      <DrawerContent>
        <Content
          key={collection.id}
          collection={collection}
          focusRef={focusRef}
          setOpen={setOpen}
          onSubmit={onSubmit}
        />
      </DrawerContent>
    </Drawer>
  );
};

export default memo(CollectionEditor);
