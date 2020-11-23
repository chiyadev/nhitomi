import React, { memo, useState } from "react";
import { Book } from "nhitomi-api";
import { AspectRatio, Box, Link, SlideFade, Text } from "@chakra-ui/react";
import BookImage from "../BookImage";
import NextLink from "next/link";
import { useBookContent } from "../../utils/book";

const Item = ({ book }: { book: Book }) => {
  const [hover, setHover] = useState(false);
  const [focus, setFocus] = useState(false);

  const content = useBookContent(book);

  return (
    <NextLink href={`/books/${book.id}/contents/${content.id}`} passHref>
      <Link
        borderRadius={4}
        overflow="hidden"
        position="relative"
        onMouseEnter={() => setHover(true)}
        onMouseLeave={() => setHover(false)}
        onFocus={() => setFocus(true)}
        onBlur={() => setFocus(false)}
      >
        <AspectRatio ratio={13 / 19}>
          <BookImage book={book} content={content} index={-1} objectFit="cover" />
        </AspectRatio>

        <SlideFade in={hover || focus}>
          <Box position="absolute" bottom={0} left={0} w="full" bg="white" color="black" p={1} opacity={0.9}>
            <Text fontWeight="bold">{book.primaryName}</Text>

            {book.primaryName !== book.englishName && <Text fontSize={12}>{book.englishName}</Text>}
          </Box>
        </SlideFade>
      </Link>
    </NextLink>
  );
};

export default memo(Item);
