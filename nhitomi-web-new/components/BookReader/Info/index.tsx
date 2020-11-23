import React, { memo } from "react";
import styles from "./index.module.css";
import { Book, BookContent } from "nhitomi-api";
import { AspectRatio, Box, Flex, Heading, Link, VStack } from "@chakra-ui/react";
import BookImage from "../../BookImage";
import NextLink from "next/link";
import Tags from "./Tags";

const Info = ({ book, content }: { book: Book; content: BookContent }) => {
  return (
    <Flex className={styles.container} w="full" p={4}>
      <Box className={styles.image}>
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
      </Box>

      <Box className={styles.info}>
        <VStack align="start" spacing={4}>
          <VStack align="start" spacing={1}>
            <NextLink href={`/books?query=${encodeURIComponent(book.primaryName)}`} passHref>
              <Link>
                <Heading>{book.primaryName}</Heading>
              </Link>
            </NextLink>

            {book.englishName && (
              <NextLink href={`/books?query=${encodeURIComponent(book.englishName)}`} passHref>
                <Link>
                  <Heading size="sm">{book.englishName}</Heading>
                </Link>
              </NextLink>
            )}
          </VStack>

          <Tags book={book} content={content} />
        </VStack>
      </Box>
    </Flex>
  );
};

export default memo(Info);
