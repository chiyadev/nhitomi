import React, { memo, ReactNode } from "react";
import { Book, BookContent, BookTag } from "nhitomi-api";
import { chakra, Link, Tag, VStack, Wrap, WrapItem } from "@chakra-ui/react";
import { BookTagColors, BookTags } from "../../../utils/constants";
import NextLink from "next/link";
import SourceList from "./SourceList";

const TagList = ({ book, content }: { book: Book; content: BookContent }) => {
  return (
    <>
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
        <SourceList book={book} selectedContent={content} />
      </Row>
    </>
  );
};

const Row = ({ name, children }: { name: ReactNode; children?: ReactNode }) => (
  <VStack align="start" spacing={1}>
    <chakra.div color="gray.500">{name}</chakra.div>
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
          <Tag colorScheme={color} fontSize="md">
            {value}
          </Tag>
        </Link>
      </NextLink>
    </WrapItem>
  );
};

export default memo(TagList);
