import React, { memo, ReactNode } from "react";
import { Icon, Link, Tooltip } from "@chakra-ui/react";
import NextLink from "next/link";

const LinkItem = ({ name, icon, href }: { name: ReactNode; icon: any; href: string }) => {
  return (
    <Tooltip label={name}>
      <span>
        <NextLink href={href} passHref>
          <Link>
            <Icon as={icon} />
          </Link>
        </NextLink>
      </span>
    </Tooltip>
  );
};

export default memo(LinkItem);
