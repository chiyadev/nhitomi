import React, { memo } from "react";
import { chakra, Icon, VStack } from "@chakra-ui/react";
import { FaTimes } from "react-icons/fa";
import { useT } from "../../locales";

const EmptyDisplay = () => {
  const t = useT();

  return (
    <VStack spacing={2}>
      <Icon as={FaTimes} fontSize="xl" />
      <chakra.div fontSize="sm">{t("BookListing.EmptyDisplay.text")}</chakra.div>
    </VStack>
  );
};

export default memo(EmptyDisplay);
