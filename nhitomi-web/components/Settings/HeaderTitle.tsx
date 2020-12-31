import React, { memo } from "react";
import { chakra, Heading, HStack, Link } from "@chakra-ui/react";
import NextLink from "next/link";
import { useT } from "../../locales";

const HeaderTitle = () => {
  const t = useT();

  return (
    <HStack align="baseline" spacing={2}>
      <NextLink href="/settings" passHref>
        <Link>
          <Heading size="md" isTruncated>
            {t("Settings.HeaderTitle.title")}
          </Heading>
        </Link>
      </NextLink>

      <chakra.div color="gray.500" fontSize="sm" isTruncated>
        {t("Settings.HeaderTitle.description")}
      </chakra.div>
    </HStack>
  );
};

export default memo(HeaderTitle);
