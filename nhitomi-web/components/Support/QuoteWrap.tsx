import React, { memo, ReactNode } from "react";
import { chakra, HStack, VStack } from "@chakra-ui/react";

const QuoteWrap = ({ children }: { children?: ReactNode }) => {
  return (
    <VStack align="stretch" spacing={8}>
      <HStack spacing={2}>
        <chakra.div alignSelf="flex-start" fontSize="xl">
          “
        </chakra.div>

        <VStack flex={1} align="start" spacing={2}>
          {children}
        </VStack>

        <chakra.div alignSelf="flex-end" fontSize="xl">
          ”
        </chakra.div>
      </HStack>

      <chakra.div color="gray.500" fontSize="sm" textAlign="right">
        &mdash; chiya.dev
      </chakra.div>
    </VStack>
  );
};

export default memo(QuoteWrap);
