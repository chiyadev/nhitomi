import React, { memo, ReactNode } from "react";
import { Book, BookContent, BookTag } from "nhitomi-api";
import { Box, Link, Tag, VStack, Wrap, WrapItem } from "@chakra-ui/react";
import { BookTags } from "../../../utils/constants";
import { BookTagColors } from "../../../utils/colors";
import NextLink from "next/link";
import Sources from "./Sources";

const Tags = ({ book, content }: { book: Book; content: BookContent }) => {
  return (
    <VStack align="start" spacing={4}>
      {BookTags.map((tag) => {
        const values = book.tags[tag];

        if (!values?.length) {
          return null;
        }

        return (
          <Row key={tag} name={tag}>
            <Wrap spacing={1}>
              {values.map((value) => (
                <Item key={value} tag={tag} value={value} />
              ))}
            </Wrap>
          </Row>
        );
      })}

      <Row name="sources">
        <Sources book={book} selectedContent={content} />
      </Row>
    </VStack>
  );
};

const Row = ({ name, children }: { name: ReactNode; children?: ReactNode }) => (
  <VStack align="start" spacing={1}>
    <Box color="gray.500" fontSize="sm">
      {name}
    </Box>

    {children}
  </VStack>
);

const Item = ({ tag, value }: { tag: BookTag; value: string }) => {
  const color = BookTagColors[tag];
  const query = `${tag}:${value.replace(/ /g, "_")}`;

  return (
    <WrapItem>
      <NextLink href={`/books?query=${encodeURIComponent(query)}`} passHref>
        <Link borderRadius="md" textTransform="capitalize">
          <Tag colorScheme={color}>{value}</Tag>
        </Link>
      </NextLink>
    </WrapItem>
  );
};

export default memo(Tags);
