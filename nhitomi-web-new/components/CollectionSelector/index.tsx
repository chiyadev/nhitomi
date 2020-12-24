import React, { Dispatch, memo, useRef } from "react";
import { Drawer, DrawerContent, DrawerOverlay } from "@chakra-ui/modal";
import Content from "./Content";
import { Collection } from "nhitomi-api";

const CollectionSelector = ({
  open,
  setOpen,
  userId,
  onSelect,
  onFilter,
}: {
  open: boolean;
  setOpen: Dispatch<boolean>;
  userId: string;
  onSelect?: Dispatch<Collection>;
  onFilter?: (collection: Collection) => boolean;
}) => {
  const focusRef = useRef<HTMLInputElement>(null);

  return (
    <Drawer placement="right" isOpen={open} onClose={() => setOpen(false)} initialFocusRef={focusRef}>
      <DrawerOverlay />
      <DrawerContent>
        <Content key={userId} focusRef={focusRef} userId={userId} onSelect={onSelect} onFilter={onFilter} />
      </DrawerContent>
    </Drawer>
  );
};

export default memo(CollectionSelector);
