import React, { ComponentProps, memo } from "react";
import { Book, BookContent } from "nhitomi-api";
import { useBookImage, useContent } from "../utils/book";
import { useBlobUrl } from "../utils/image";
import { Box, Center } from "@chakra-ui/layout";
import { Spinner } from "@chakra-ui/spinner";
import Icon from "@chakra-ui/icon";
import { Fade, ScaleFade } from "@chakra-ui/transition";

const BookImage = ({
  book,
  content,
  index,
  ...props
}: {
  book: Book;
  content?: BookContent;
  index: number;
} & ComponentProps<typeof Box>) => {
  const defaultContent = useContent(book);
  content = content || defaultContent;

  const image = useBookImage(book, content, index);
  const url = useBlobUrl(image instanceof Error ? undefined : image);

  if (!url) {
    return (
      <Fade in>
        <Center {...props}>
          <Icon as={Spinner} />
        </Center>
      </Fade>
    );
  }

  return (
    <ScaleFade in>
      <Box as="img" alt={`${book.id}/${content.id}/${index}`} src={url} h="full" {...props} />
    </ScaleFade>
  );
};

export default memo(BookImage);
