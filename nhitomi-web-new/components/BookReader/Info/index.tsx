import React, { memo, ReactNode } from "react";
import styles from "./index.module.css";
import { Book, BookContent } from "nhitomi-api";
import { AspectRatio, chakra, Flex, Heading, HStack, Icon, Link, VStack } from "@chakra-ui/react";
import BookImage from "../../BookImage";
import NextLink from "next/link";
import TagList from "./TagList";
import { AiOutlineHistory, AiOutlineRead, AiOutlineUpload } from "react-icons/ai";
import { useT } from "../../../locales";
import DateDisplay from "../../DateDisplay";

const Info = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();

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

          <chakra.div color="gray.200">
            <InfoLine icon={AiOutlineRead}>{t("BookReader.Info.pageCount", { count: content.pageCount })}</InfoLine>
            <InfoLine icon={AiOutlineUpload}>
              {t("BookReader.Info.createdTime", { time: <DateDisplay date={book.createdTime} /> })}
            </InfoLine>
            <InfoLine icon={AiOutlineHistory}>
              {t("BookReader.Info.updatedTime", { time: <DateDisplay date={book.updatedTime} /> })}
            </InfoLine>
          </chakra.div>
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
