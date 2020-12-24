import React, { Dispatch, memo, useRef } from "react";
import { Book, BookContent } from "nhitomi-api";
import { Modal, ModalContent, ModalOverlay } from "@chakra-ui/react";
import Content from "./Content";

export type DownloadTarget = {
  book: Book;
  content: BookContent;
};

const BookDownloader = ({
  targets,
  open,
  setOpen,
  autoClose = targets.length <= 1,
}: {
  targets: DownloadTarget[];
  open: boolean;
  setOpen: Dispatch<boolean>;
  autoClose?: boolean;
}) => {
  const focusRef = useRef<HTMLButtonElement>(null);

  return (
    <Modal
      size="2xl"
      isOpen={open}
      onClose={() => setOpen(false)}
      isCentered
      scrollBehavior="inside"
      closeOnEsc={false}
      closeOnOverlayClick={false}
      initialFocusRef={focusRef}
    >
      <ModalOverlay />
      <ModalContent>
        <Content focusRef={focusRef} targets={targets} setOpen={setOpen} autoClose={autoClose} />
      </ModalContent>
    </Modal>
  );
};

export default memo(BookDownloader);
