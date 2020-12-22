import React, { ComponentProps, memo } from "react";
import { Button, chakra, VStack } from "@chakra-ui/react";

const ImageRadioButton = ({
  src,
  isChecked,
  children,
  ...props
}: {
  src?: string;
  isChecked?: boolean;
} & ComponentProps<typeof Button>) => {
  return (
    <Button variant={isChecked ? "solid" : "ghost"} p={2} h={undefined} fontWeight="normal" {...props}>
      <VStack>
        <chakra.img src={src} w={28} />
        <div>{children}</div>
      </VStack>
    </Button>
  );
};

export default memo(ImageRadioButton);
