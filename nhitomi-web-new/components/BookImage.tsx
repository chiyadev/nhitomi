import React, { ComponentProps, memo } from "react";
import { Book, BookContent } from "nhitomi-api";
import { useBookImage, useBookContent } from "../utils/book";
import { useBlobUrl } from "../utils/image";
import { Box, Center, Fade, Icon, ScaleFade, Spinner } from "@chakra-ui/react";
import { useTimerOnce } from "../utils/time";
import SimpleLazy from "./SimpleLazy";

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
  return (
    <SimpleLazy
      render={(load) => {
        return load ? <Content book={book} content={content} index={index} {...props} /> : <Box {...props} />;
      }}
    />
  );
};

const Content = ({ book, content, index, ...props }: ComponentProps<typeof BookImage>) => {
  const defaultContent = useBookContent(book);
  content = content || defaultContent;

  const image = useBookImage(book, content, index);
  const url = useBlobUrl(image instanceof Error ? undefined : image);
  const showLoading = useTimerOnce(3000);

  if (!url) {
    return (
      <Fade in={showLoading}>
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
