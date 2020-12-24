import React, { memo } from "react";
import { Heading, Link } from "@chakra-ui/react";
import { User } from "nhitomi-api";
import NextLink from "next/link";
import { useClientInfoAuth } from "../../utils/client";
import { useT } from "../../locales";

const HeaderTitle = ({ user }: { user: User }) => {
  const t = useT();
  const info = useClientInfoAuth();

  return (
    <Heading size="md" isTruncated p={1} m={-1}>
      {info?.user.id === user.id
        ? t("CollectionListing.title")
        : t("CollectionListing.titleUser", {
            user: (
              <NextLink href={`/users/${user.id}`} passHref>
                <Link>{user.username}</Link>
              </NextLink>
            ),
          })}
    </Heading>
  );
};

export default memo(HeaderTitle);
