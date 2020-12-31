import React, { memo, useMemo } from "react";
import { Book, BookContent } from "nhitomi-api";
import { Center, chakra, Divider, HStack, Icon, Link, VStack } from "@chakra-ui/react";
import { useT } from "../../../../locales";
import { FaBook, FaChevronRight, FaHeart } from "react-icons/fa";
import { useBookContentSelector } from "../../../../utils/book";
import { ScraperIcons, ScraperTypes } from "../../../../utils/constants";
import { useClientInfo } from "../../../../utils/client";
import NextLink from "next/link";
import { LayoutResult, LayoutRow } from "./layoutEngine";

function getRowRectangle(row: LayoutRow) {
  const top = Math.min(...row.images.map((image) => image.y));
  const left = Math.min(...row.images.map((image) => image.x));
  const bottom = Math.max(...row.images.map((image) => image.y + image.height));
  const right = Math.max(...row.images.map((image) => image.x + image.width));

  return { x: left, y: top, width: right - left, height: bottom - top };
}

const PageLimited = ({
  layout,
  book,
  selectedContent,
}: {
  layout: LayoutResult;
  book: Book;
  selectedContent: BookContent;
}) => {
  const t = useT();
  const info = useClientInfo();
  const selectContent = useBookContentSelector();

  const rect = useMemo(() => getRowRectangle(layout.rows[layout.rows.length - 1]), [layout]);

  return (
    <Center
      px={2}
      bg="rgba(0, 0, 0, 0.75)"
      style={useMemo(
        () => ({
          position: "absolute",
          transform: `translate(${rect.x}px, ${rect.y}px)`,
          width: rect.width,
          height: rect.height,
        }),
        [rect.x, rect.y, rect.width, rect.height]
      )}
    >
      <VStack
        align="stretch"
        spacing={4}
        p={4}
        w="full"
        maxW="lg"
        bg="gray.700"
        borderRadius="md"
        boxShadow="lg"
        divider={<Divider />}
      >
        <HStack>
          <Icon as={FaBook} />
          <div>{t("BookReader.PageDisplay.PageLimited.title")}</div>
        </HStack>

        {ScraperTypes.filter((source) => book.contents.find((c) => c.source === source)).map((source) => {
          const content =
            selectedContent.source === source
              ? selectedContent
              : selectContent(book.contents.filter((content) => content.source === source));

          return (
            <HStack key={source} spacing={2}>
              <chakra.img src={ScraperIcons[source]} w={8} borderRadius="full" />

              <VStack align="start" spacing={0} flex={1} minW={0}>
                <Link href={content.sourceUrl} isExternal>
                  {info?.scrapers.find((scraper) => scraper.type === source)?.name}
                </Link>
                <chakra.div maxW="full" fontSize="sm" color="gray.500" isTruncated>
                  {content.sourceUrl}
                </chakra.div>
              </VStack>

              <Icon as={FaChevronRight} />
            </HStack>
          );
        })}

        <HStack spacing={2}>
          <Icon as={FaHeart} color="pink.300" fontSize="xl" w={8} />

          <VStack align="start" spacing={0} flex={1} minW={0}>
            <NextLink href="/support" passHref>
              <Link>{t("BookReader.PageDisplay.PageLimited.support")}</Link>
            </NextLink>
            <chakra.div fontSize="sm" color="gray.500">
              {t("BookReader.PageDisplay.PageLimited.supportDescription")}
            </chakra.div>
          </VStack>

          <Icon as={FaChevronRight} />
        </HStack>
      </VStack>
    </Center>
  );
};

export default memo(PageLimited);
