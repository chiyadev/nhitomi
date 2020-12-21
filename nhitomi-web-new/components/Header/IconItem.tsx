import React, { memo, ReactNode } from "react";
import { Icon, Link, Tooltip } from "@chakra-ui/react";

const IconItem = ({ name, icon, onClick }: { name: ReactNode; icon: any; onClick?: () => void }) => {
  return (
    <Tooltip label={name}>
      <Link as="button" onClick={onClick}>
        <Icon as={icon} />
      </Link>
    </Tooltip>
  );
};

export default memo(IconItem);
