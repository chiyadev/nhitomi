import React, { memo } from "react";
import { Box, Icon, Tooltip } from "@chakra-ui/react";

const IconItem = ({ name, icon, onClick }: { name: string; icon: any; onClick?: () => void }) => {
  return (
    <Tooltip label={name}>
      <Box m={4} cursor="pointer" onClick={onClick}>
        <Icon as={icon} />
      </Box>
    </Tooltip>
  );
};

export default memo(IconItem);
