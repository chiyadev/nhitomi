import React, { memo } from "react";
import { Book, BookContent } from "nhitomi-api";
import { Heading, HStack, Link } from "@chakra-ui/react";
import NextLink from "next/link";

const HeaderTitle = ({ book, content }: { book: Book; content: BookContent }) => {
  const artist = book.tags.artist?.[0];

  return (
    <HStack align="baseline" spacing={2}>
      <NextLink href={`/books/${book.id}/contents/${content.id}`} passHref>
        <Heading as={Link} size="md" isTruncated>
          {book.primaryName}
        </Heading>
      </NextLink>

      {artist && (
        <NextLink href={{ pathname: "/books", query: { query: `artist:${artist.replace(/ /g, "_")}` } }} passHref>
          <Heading as={Link} size="sm" color="gray.500" isTruncated>
            {artist}
          </Heading>
        </NextLink>
      )}
    </HStack>
  );
};

export default memo(HeaderTitle);
