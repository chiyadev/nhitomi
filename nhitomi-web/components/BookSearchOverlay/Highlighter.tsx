import React, { Fragment, memo, useMemo } from "react";
import { chakra } from "@chakra-ui/react";
import { BookTagColors } from "../../utils/constants";
import { tokenizeBookQuery } from "../../utils/book";

const Highlighter = ({ text }: { text: string }) => {
  const tokens = useMemo(() => tokenizeBookQuery(text), [text]);

  return (
    <chakra.div whiteSpace="pre">
      {tokens.map((token) => {
        switch (token.type) {
          case "url":
            return (
              <chakra.span key={token.index} color="cyan.300">
                {token.text}
              </chakra.span>
            );

          case "tag":
            return (
              <Fragment key={token.index}>
                <chakra.span color="gray.500">{token.tag}:</chakra.span>
                <chakra.span color={`${BookTagColors[token.tag]}.300`}>{token.value}</chakra.span>
              </Fragment>
            );

          default:
            return <span key={token.index}>{token.text}</span>;
        }
      })}
    </chakra.div>
  );
};

export default memo(Highlighter);
