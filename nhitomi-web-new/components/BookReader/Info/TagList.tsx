import React, { memo, ReactNode } from "react";
import { Book, BookContent, BookTag } from "nhitomi-api";
import { chakra, Link, Tag, VStack, Wrap, WrapItem } from "@chakra-ui/react";
import { BookTagColors, BookTags } from "../../../utils/constants";
import NextLink from "next/link";
import SourceList from "./SourceList";
import { useT } from "../../../locales";

const TagList = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();

  return (
    <>
      {BookTags.map((tag) => {
        const values = book.tags[tag];

        if (!values?.length) {
          return null;
        }

        return (
          <Row key={tag} name={t("BookTag", { value: tag })}>
            <Wrap spacing={1}>
              {values.map((value) => (
                <Item key={value} tag={tag} value={value} />
              ))}
            </Wrap>
          </Row>
        );
      })}

      <Row name={t("BookReader.Info.TagList.sources")}>
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

  return (
    <WrapItem>
      <NextLink href={{ pathname: "/books", query: { query: `${tag}:${value.replace(/ /g, "_")}` } }} passHref>
        <Link color={`${color}.200`} borderRadius="md" textTransform="capitalize">
          <Tag colorScheme={color} fontSize="md">
            {value}
          </Tag>
        </Link>
      </NextLink>
    </WrapItem>
  );
};

export default memo(TagList);
