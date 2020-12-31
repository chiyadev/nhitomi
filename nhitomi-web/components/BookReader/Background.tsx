import React, { memo } from "react";
import { Book, BookContent } from "nhitomi-api";
import BookImage from "../BookImage";
import { Portal } from "@chakra-ui/react";

const Background = ({ book, content, visible }: { book: Book; content: BookContent; visible: boolean }) => {
  return (
    <Portal>
      <BookImage
        book={book}
        content={content}
        index={-1}
        position="fixed"
        top={0}
        left={0}
        w="full"
        h="full"
        zIndex={-1}
        bg="black"
        pointerEvents="none"
        filter="blur(1rem)"
        transform="scale(1.1)"
        objectFit="cover"
        objectPosition="center"
        opacity={visible ? 0.1 : 0}
        transition="opacity 0.5s"
      />
    </Portal>
  );
};

export default memo(Background);
