import React, { Dispatch, memo, useRef } from "react";
import { Book, Collection } from "nhitomi-api";
import { Drawer, DrawerContent, DrawerOverlay } from "@chakra-ui/react";
import Content from "./Content";

const CollectionItemSelector = ({
  open,
  setOpen,
  collection,
  onSelect,
}: {
  open: boolean;
  setOpen: Dispatch<boolean>;
  collection: Collection;
  onSelect?: Dispatch<Book[]>;
}) => {
  const focusRef = useRef<HTMLInputElement>(null);

  return (
    <Drawer placement="right" isOpen={open} onClose={() => setOpen(false)} initialFocusRef={focusRef}>
      <DrawerOverlay />
      <DrawerContent>
        <Content
          key={collection.id}
          focusRef={focusRef}
          collection={collection}
          setOpen={setOpen}
          onSelect={onSelect}
        />
      </DrawerContent>
    </Drawer>
  );
};

export default memo(CollectionItemSelector);
