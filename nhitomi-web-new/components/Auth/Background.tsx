import { chakra, Portal } from "@chakra-ui/react";
import React, { memo } from "react";
import BackgroundSrc from "../../assets/Auth/Background.jpg";

const Background = () => {
  return (
    <Portal>
      <chakra.img
        src={BackgroundSrc}
        position="fixed"
        top={0}
        left={0}
        w="full"
        h="full"
        zIndex={-1}
        pointerEvents="none"
        transform="scale(1.1)"
        objectFit="cover"
        objectPosition="center"
        opacity={0.1}
      />
    </Portal>
  );
};

export default memo(Background);
