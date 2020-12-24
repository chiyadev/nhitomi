import React, { memo, useMemo, useState } from "react";
import { Book } from "nhitomi-api";
import { AspectRatio, chakra, Link, SlideFade, Text } from "@chakra-ui/react";
import BookImage from "../BookImage";
import NextLink from "next/link";
import { useBookContent } from "../../utils/book";
import { useConfig } from "../../utils/config";

const Item = ({ book }: { book: Book }) => {
  const content = useBookContent(book);

  const [forceOverlay] = useConfig("bookForceInfoOverlay");
  const [hover, setHover] = useState(false);
  const [focus, setFocus] = useState(false);

  return (
    <NextLink href={`/books/${book.id}/contents/${content.id}`} passHref>
      <Link
        borderRadius="md"
        overflow="hidden"
        position="relative"
        onMouseEnter={() => setHover(true)}
        onMouseMove={() => setHover(true)}
        onMouseLeave={() => setHover(false)}
        onTouchStart={() => setHover(true)}
        onTouchEnd={() => setHover(false)}
        onFocus={() => setFocus(true)}
        onBlur={() => setFocus(false)}
      >
        <AspectRatio ratio={13 / 19}>
          <BookImage
            book={book}
            content={content}
            index={-1}
            intersection={useMemo(() => ({ rootMargin: "100%" }), [])}
            animateIn="scale"
            objectFit="cover"
            objectPosition="center"
          />
        </AspectRatio>

        <SlideFade in={forceOverlay || hover || focus}>
          <chakra.div position="absolute" bottom={0} left={0} w="full" bg="white" color="black" p={1} opacity={0.9}>
            <Text fontWeight="bold">{book.primaryName}</Text>

            {book.primaryName !== book.englishName && <Text fontSize="sm">{book.englishName}</Text>}
          </chakra.div>
        </SlideFade>
      </Link>
    </NextLink>
  );
};

export default memo(Item);
