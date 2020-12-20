import { chakra } from "@chakra-ui/react";
import React, { memo, ReactNode } from "react";

const LayoutBody = ({ children }: { children?: ReactNode }) => {
  return (
    <chakra.div maxW="1200px" mx="auto">
      {children}
    </chakra.div>
  );
};

export default memo(LayoutBody);
