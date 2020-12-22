import React, { memo } from "react";
import { Center, Modal, ModalContent, ModalOverlay, Spinner } from "@chakra-ui/react";

const nop = () => {};

const BlockingSpinner = ({ visible, onCancel }: { visible?: boolean; onCancel?: () => void }) => {
  return (
    <Modal isOpen={visible || false} onClose={onCancel || nop} isCentered>
      <ModalOverlay />
      <Center as={ModalContent} w={16} h={16}>
        <Spinner />
      </Center>
    </Modal>
  );
};

export default memo(BlockingSpinner);
