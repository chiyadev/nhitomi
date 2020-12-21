import React, { Fragment, memo, useMemo } from "react";
import { tokenizeBookQuery } from "../../utils/book";
import { chakra, Link } from "@chakra-ui/react";
import { BookTagColors } from "../../utils/constants";
import NextLink from "next/link";

const HeaderTitleQuery = ({ query }: { query: string }) => {
  const tokens = useMemo(() => tokenizeBookQuery(query), [query]);

  return (
    <chakra.div whiteSpace="pre" isTruncated margin={-1} padding={1}>
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
                <chakra.span color="gray.500">{token.tag}:</chakra.span>

                <NextLink href={{ pathname: "/books", query: { query: `${token.tag}:${token.value}` } }} passHref>
                  <Link color={`${BookTagColors[token.tag]}.300`}>
                    <strong>{token.value}</strong>
                  </Link>
                </NextLink>
              </Fragment>
            );

          default:
            return <span key={token.index}>{token.text}</span>;
        }
      })}
    </chakra.div>
  );
};

export default memo(HeaderTitleQuery);
