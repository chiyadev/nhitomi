import React, { memo, ReactNode } from "react";
import styles from "./index.module.css";
import { Book, BookContent } from "nhitomi-api";
import { AspectRatio, chakra, Flex, Heading, HStack, Icon, Link, VStack } from "@chakra-ui/react";
import BookImage from "../../BookImage";
import NextLink from "next/link";
import TagList from "./TagList";
import { AiOutlineHistory, AiOutlineRead, AiOutlineUpload } from "react-icons/ai";

const Info = ({ book, content }: { book: Book; content: BookContent }) => {
  return (
    <Flex className={styles.container} w="full" p={4}>
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
            <NextLink href={`/books?query=${encodeURIComponent(book.primaryName)}`} passHref>
              <Link>
                <Heading>{book.primaryName}</Heading>
              </Link>
            </NextLink>

            {book.englishName && (
              <NextLink href={`/books?query=${encodeURIComponent(book.englishName)}`} passHref>
                <Link>
                  <Heading size="md">{book.englishName}</Heading>
                </Link>
              </NextLink>
            )}
          </VStack>

          <TagList book={book} content={content} />

          <VStack align="start" spacing={0} color="gray.200">
            <InfoLine icon={AiOutlineRead}>{content.pageCount} pages</InfoLine>
            <InfoLine icon={AiOutlineUpload}>Uploaded on {book.createdTime.toDateString()}</InfoLine>
            <InfoLine icon={AiOutlineHistory}>Updated on {book.updatedTime.toDateString()}</InfoLine>
          </VStack>
        </VStack>
      </chakra.div>
    </Flex>
  );
};

const InfoLine = ({ icon, children }: { icon: any; children?: ReactNode }) => {
  return (
    <HStack spacing={1}>
      <Icon as={icon} fontSize="lg" />
      <div>{children}</div>
    </HStack>
  );
};

export default memo(Info);
