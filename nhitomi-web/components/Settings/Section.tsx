import React, { memo, ReactNode } from "react";
import { Divider, HStack, Icon, VStack } from "@chakra-ui/react";

const Section = ({ icon, name, children }: { icon: any; name: ReactNode; children?: ReactNode }) => {
  return (
    <VStack align="stretch" spacing={4}>
      <HStack spacing={2} color="gray.500" fontSize="sm">
        <Icon as={icon} />
        <div>{name}</div>
      </HStack>

      <VStack align="stretch" spacing={4} divider={<Divider />}>
        {children}
      </VStack>
    </VStack>
  );
};

export default memo(Section);
