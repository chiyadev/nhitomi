import React, { Dispatch, memo, useRef } from "react";
import { Modal, ModalContent, ModalOverlay } from "@chakra-ui/react";
import Content from "./Content";
import { Collection } from "nhitomi-api";

const CollectionCreator = ({
  open,
  setOpen,
  onCreate,
}: {
  open: boolean;
  setOpen: Dispatch<boolean>;
  onCreate?: Dispatch<Collection>;
}) => {
  const focusRef = useRef<HTMLInputElement>(null);

  return (
    <Modal isOpen={open} onClose={() => setOpen(false)} isCentered initialFocusRef={focusRef}>
      <ModalOverlay />
      <ModalContent>
        <Content focusRef={focusRef} setOpen={setOpen} onCreate={onCreate} />
      </ModalContent>
    </Modal>
  );
};

export default memo(CollectionCreator);
