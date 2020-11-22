import React, { Fragment, memo, useMemo } from "react";
import { Box, chakra } from "@chakra-ui/react";
import { tokenizeQuery } from "../query";
import { BookTagColors } from "../../../utils/colors";

const Highlighter = ({ text }: { text: string }) => {
  const tokens = useMemo(() => tokenizeQuery(text), [text]);

  return (
    <Box whiteSpace="pre">
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
    </Box>
  );
};

export default memo(Highlighter);
