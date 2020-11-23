import React, { memo } from "react";
import { Book, BookContent } from "nhitomi-api";
import BookImage from "../BookImage";
import { Box, Portal } from "@chakra-ui/react";
import styles from "./Background.module.css";

const Background = ({ book, content }: { book: Book; content: BookContent }) => {
  return (
    <Portal>
      <Box
        className={styles.container}
        position="fixed"
        top={0}
        w="full"
        h="full"
        zIndex={-1}
        pointerEvents="none"
        opacity={0.1}
      >
        <BookImage book={book} content={content} index={-1} objectFit="cover" />
      </Box>
    </Portal>
  );
};

export default memo(Background);
