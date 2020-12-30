import React, { memo } from "react";
import { Center, chakra, Flex, HStack, Icon, Link } from "@chakra-ui/react";
import ChinoBookHolder from "../../assets/Support/ChinoBookHolder.png";
import BookImage from "../BookImage";
import { Book, BookContent } from "nhitomi-api";
import NextLink from "next/link";
import { FaHeart } from "react-icons/fa";
import { useT } from "../../locales";

const SupportBanner = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();

  return (
    <Center px={2}>
      <NextLink href="/support" passHref>
        <Link w="full" maxW="xl" overflow="hidden" borderRadius="md">
          <Flex w="full" h={20} bg="white" color="black" boxShadow="lg">
            <Center flex={0.4}>
              <chakra.div position="relative" pointerEvents="none" mt={6}>
                <BookImage
                  book={book}
                  content={content}
                  index={-1}
                  position="absolute"
                  transform="translate(115%, 136%) rotate(1.6deg)"
                  w="31%"
                  h="31%"
                  objectFit="cover"
                />

                <chakra.img position="relative" src={ChinoBookHolder} />
              </chakra.div>
            </Center>

            <HStack flex={0.6} spacing={2} ml={-4}>
              <Icon as={FaHeart} fontSize="2xl" color="pink.300" transform="rotate(20deg)" />
              <chakra.div>
                {t("BookReader.SupportBanner.heading", {
                  name: <chakra.span fontWeight="bold">nhitomi</chakra.span>,
                })}
              </chakra.div>
            </HStack>
          </Flex>
        </Link>
      </NextLink>
    </Center>
  );
};

export default memo(SupportBanner);
