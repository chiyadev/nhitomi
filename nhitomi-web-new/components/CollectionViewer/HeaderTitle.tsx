import React, { memo } from "react";
import { chakra, Heading, HStack, Link } from "@chakra-ui/react";
import { Collection } from "nhitomi-api";
import NextLink from "next/link";

const HeaderTitle = ({ collection }: { collection: Collection }) => {
  return (
    <HStack align="baseline" spacing={2}>
      <NextLink href={`/collections/${collection.id}`} passHref>
        <Heading as={Link} size="md" isTruncated>
          {collection.name}
        </Heading>
      </NextLink>

      {collection.description && (
        <chakra.div fontSize="sm" color="gray.500" isTruncated>
          {collection.description}
        </chakra.div>
      )}
    </HStack>
  );
};

export default memo(HeaderTitle);
