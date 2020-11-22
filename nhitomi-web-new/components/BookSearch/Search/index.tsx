import React, { memo } from "react";
import { Modal, ModalContent, ModalOverlay } from "@chakra-ui/react";
import Content from "./Content";

const Search = ({
  value,
  setValue,
  open,
  setOpen,
}: {
  value: string;
  setValue: (value: string) => Promise<void>;
  open: boolean;
  setOpen: (value: boolean) => void;
}) => {
  return (
    <Modal isOpen={open} onClose={() => setOpen(false)} motionPreset="slideInBottom" size="xl">
      <ModalOverlay />
      <ModalContent ml={4} mr={4} mt={8}>
        <Content value={value} setValue={setValue} />
      </ModalContent>
    </Modal>
  );
};

export default memo(Search);
