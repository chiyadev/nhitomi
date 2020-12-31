import React, { memo } from "react";
import { chakra, HStack, Icon, Link, VStack } from "@chakra-ui/react";
import { FaFolderOpen, FaPlus } from "react-icons/fa";
import { useT } from "../../../locales";
import NextLink from "next/link";

const EmptyDisplay = () => {
  const t = useT();

  return (
    <VStack spacing={2}>
      <Icon as={FaFolderOpen} fontSize="xl" />

      <chakra.div fontSize="sm">{t("CollectionViewer.Book.EmptyDisplay.text")}</chakra.div>

      <NextLink href="/books" passHref>
        <Link color="blue.300" fontSize="sm">
          <HStack spacing={2}>
            <Icon as={FaPlus} />
            <div>{t("CollectionViewer.Book.EmptyDisplay.add")}</div>
          </HStack>
        </Link>
      </NextLink>
    </VStack>
  );
};

export default memo(EmptyDisplay);
