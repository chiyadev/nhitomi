import React, { memo } from "react";
import { chakra, Heading, HStack, Link } from "@chakra-ui/react";
import NextLink from "next/link";
import Logo40x40 from "../../assets/logo-40x40.png";

const HeaderTitleApp = () => {
  return (
    <HStack>
      <NextLink href="/" passHref>
        <Link>
          <HStack spacing={2}>
            <chakra.img src={Logo40x40} w={6} />
            <Heading size="md">nhitomi</Heading>
          </HStack>
        </Link>
      </NextLink>
    </HStack>
  );
};

export default memo(HeaderTitleApp);
