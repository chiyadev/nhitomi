import React, { Fragment, memo, useMemo } from "react";
import { tokenizeBookQuery } from "../../utils/book";
import { chakra, HStack, Link, Icon, Tooltip } from "@chakra-ui/react";
import { BookTagColors } from "../../utils/constants";
import { FaTimes } from "react-icons/fa";
import NextLink from "next/link";
import { useT } from "../../locales";

const HeaderTitleQuery = ({ query }: { query: string }) => {
  const t = useT();
  const tokens = useMemo(() => tokenizeBookQuery(query), [query]);

  return (
    <HStack spacing={2}>
      <Tooltip label={t("BookListing.HeaderTitleQuery.cancel")}>
        <span>
          <NextLink href="/books" passHref>
            <Link>
              <Icon as={FaTimes} />
            </Link>
          </NextLink>
        </span>
      </Tooltip>

      <chakra.div minW={0}>
        <chakra.div whiteSpace="pre" color="gray.500" isTruncated margin={-1} padding={1}>
          {tokens.map((token) => {
            switch (token.type) {
              case "url":
                return (
                  <Link key={token.index} href={token.text} color="cyan.300" isExternal>
                    <strong>{token.text}</strong>
                  </Link>
                );

              case "tag":
                return (
                  <Fragment key={token.index}>
                    <span>{token.tag}:</span>

                    <NextLink href={{ pathname: "/books", query: { query: `${token.tag}:${token.value}` } }} passHref>
                      <Link color={`${BookTagColors[token.tag]}.300`}>
                        <strong>{token.value}</strong>
                      </Link>
                    </NextLink>
                  </Fragment>
                );

              default:
                return (
                  <chakra.span key={token.index} color="white">
                    {token.text}
                  </chakra.span>
                );
            }
          })}
        </chakra.div>
      </chakra.div>
    </HStack>
  );
};

export default memo(HeaderTitleQuery);
