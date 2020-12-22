import React, { memo, ReactNode } from "react";
import { chakra, VStack } from "@chakra-ui/react";

const SectionItem = ({
  title,
  description,
  children,
}: {
  title?: ReactNode;
  description?: ReactNode;
  children?: ReactNode;
}) => {
  return (
    <VStack align="start" spacing={4}>
      <div>
        {title && <div>{title}</div>}
        {description && (
          <chakra.div color="gray.500" fontSize="sm">
            {description}
          </chakra.div>
        )}
      </div>

      {children}
    </VStack>
  );
};

export default memo(SectionItem);
