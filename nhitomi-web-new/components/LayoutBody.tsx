import { chakra } from "@chakra-ui/react";
import React, { ComponentProps, memo } from "react";

const LayoutBody = (props: ComponentProps<typeof chakra.div>) => {
  return <chakra.div maxW="1200px" mx="auto" {...props} />;
};

export default memo(LayoutBody);
