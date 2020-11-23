import React, { memo } from "react";
import { Book, BookTag } from "nhitomi-api";
import { Box, chakra, Link, VStack, Wrap, WrapItem } from "@chakra-ui/react";
import { BookTags } from "../../../utils/constants";
import { BookTagColors } from "../../../utils/colors";
import NextLink from "next/link";

const Tags = ({ book }: { book: Book }) => {
  return (
    <VStack align="start" spacing={4}>
      {BookTags.map((tag) => {
        const tags = book.tags[tag];

        if (!tags?.length) {
          return null;
        }

        return (
          <VStack key={tag} align="start" spacing={1}>
            <Box color="gray.500" fontSize="sm">
              {tag}
            </Box>
            <Wrap spacing={1}>
              {tags.map((value) => (
                <Item key={value} tag={tag} value={value} />
              ))}
            </Wrap>
          </VStack>
        );
      })}
    </VStack>
  );
};

const Item = ({ tag, value }: { tag: BookTag; value: string }) => {
  const color = BookTagColors[tag];
  const query = `${tag}:${value.replace(/ /g, "_")}`;

  return (
    <WrapItem>
      <NextLink href={`/books?query=${encodeURIComponent(query)}`} passHref>
        <Link color={`${color}.300`} pl={1} pr={1} position="relative" borderRadius="sm">
          <Box
            position="absolute"
            top={0}
            left={0}
            w="full"
            h="full"
            borderRadius="sm"
            borderWidth={1}
            borderColor={`${color}.300`}
            bg={`${color}.700`}
            opacity={0.25}
            zIndex={-1}
          />

          <chakra.span textTransform="capitalize">{value}</chakra.span>
        </Link>
      </NextLink>
    </WrapItem>
  );
};

export default memo(Tags);
