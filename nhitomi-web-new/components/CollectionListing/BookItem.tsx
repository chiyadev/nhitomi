import React, { memo, useMemo } from "react";
import { Book, Collection } from "nhitomi-api";
import { useBookContentSelector } from "../../utils/book";
import NextLink from "next/link";
import { AspectRatio, chakra, Link, Text } from "@chakra-ui/react";
import BookImage from "../BookImage";

const BookItem = ({ cover, collection }: { cover?: Book; collection: Collection }) => {
  const selectContent = useBookContentSelector();

  return (
    <NextLink href={`/collections/${collection.id}`} passHref>
      <Link borderRadius="md" overflow="hidden" position="relative">
        <AspectRatio ratio={13 / 19}>
          {cover ? (
            <BookImage
              book={cover}
              content={selectContent(cover.contents)}
              index={-1}
              intersection={useMemo(() => ({ rootMargin: "100%" }), [])}
              animateIn="scale"
              objectFit="cover"
              objectPosition="center"
            />
          ) : (
            <div />
          )}
        </AspectRatio>

        <chakra.div position="absolute" bottom={0} left={0} w="full" bg="white" color="black" p={1} opacity={0.9}>
          <Text fontWeight="bold">{collection.name}</Text>

          {collection.description && <Text fontSize="sm">{collection.description}</Text>}
        </chakra.div>
      </Link>
    </NextLink>
  );
};

export default memo(BookItem);
