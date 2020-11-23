import React, { memo, ReactNode } from "react";
import styles from "./index.module.css";
import { Book, BookContent } from "nhitomi-api";
import { AspectRatio, Box, chakra, Flex, Heading, HStack, Icon, Link, VStack } from "@chakra-ui/react";
import BookImage from "../../BookImage";
import NextLink from "next/link";
import Tags from "./Tags";
import { AiOutlineHistory, AiOutlineRead, AiOutlineUpload } from "react-icons/ai";

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

          <VStack align="start" spacing={0} color="gray.200">
            <InfoLine icon={AiOutlineRead}>{content.pageCount} pages</InfoLine>
            <InfoLine icon={AiOutlineUpload}>Uploaded on {book.createdTime.toDateString()}</InfoLine>
            <InfoLine icon={AiOutlineHistory}>Updated on {book.updatedTime.toDateString()}</InfoLine>
          </VStack>
        </VStack>
      </Box>
    </Flex>
  );
};

const InfoLine = ({ icon, children }: { icon: any; children?: ReactNode }) => {
  return (
    <HStack spacing={1}>
      <Icon as={icon} fontSize="md" />
      <chakra.div fontSize="sm">{children}</chakra.div>
    </HStack>
  );
};

export default memo(Info);
