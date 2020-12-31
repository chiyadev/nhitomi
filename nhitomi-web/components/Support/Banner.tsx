import React, { memo } from "react";
import { AspectRatio, chakra, Divider, VStack } from "@chakra-ui/react";
import MegumiEcchi from "../../assets/Support/MegumiEcchi.png";

const Banner = () => {
  return (
    <VStack align="stretch" spacing={0}>
      <AspectRatio w="full" maxW="sm" ratio={1} overflow="hidden">
        <chakra.img src={MegumiEcchi} />
      </AspectRatio>

      <Divider />
    </VStack>
  );
};

export default memo(Banner);
