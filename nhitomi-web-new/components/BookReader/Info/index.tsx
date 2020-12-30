import React, { memo } from "react";
import styles from "./index.module.css";
import { Book, BookContent } from "nhitomi-api";
import { AspectRatio, chakra, Flex, Heading, Link, VStack, Wrap, WrapItem } from "@chakra-ui/react";
import BookImage from "../../BookImage";
import NextLink from "next/link";
import TagList from "./TagList";
import FavoriteButton from "./FavoriteButton";
import DownloadButton from "./DownloadButton";
import AddToCollectionButton from "./AddToCollectionButton";
import InfoText from "./InfoText";

const Info = ({ book, content }: { book: Book; content: BookContent }) => {
  return (
    <Flex className={styles.container} p={4}>
      <div className={styles.image}>
        <AspectRatio ratio={13 / 19} maxW="sm">
          <BookImage
            book={book}
            content={content}
            index={-1}
            objectFit="cover"
            objectPosition="center"
            borderRadius="md"
          />
        </AspectRatio>
      </div>

      <chakra.div className={styles.info}>
        <VStack align="start" spacing={4}>
          <VStack align="start" spacing={1}>
            <NextLink href={{ pathname: "/books", query: { query: book.primaryName } }} passHref>
              <Link>
                <Heading>{book.primaryName}</Heading>
              </Link>
            </NextLink>

            {book.englishName && (
              <NextLink href={{ pathname: "/books", query: { query: book.englishName } }} passHref>
                <Link>
                  <Heading size="md">{book.englishName}</Heading>
                </Link>
              </NextLink>
            )}
          </VStack>

          <TagList book={book} content={content} />
          <InfoText book={book} content={content} />

          <Wrap spacing={2}>
            <WrapItem>
              <FavoriteButton book={book} content={content} />
            </WrapItem>
            <WrapItem>
              <AddToCollectionButton book={book} content={content} />
            </WrapItem>
            <WrapItem>
              <DownloadButton book={book} content={content} />
            </WrapItem>
          </Wrap>
        </VStack>
      </chakra.div>
    </Flex>
  );
};

export default memo(Info);
