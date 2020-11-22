import React, { Fragment, memo, useMemo } from "react";
import { Box } from "@chakra-ui/react";
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
              <Box key={token.index} as="span" color="cyan.300">
                {token.text}
              </Box>
            );

          case "tag":
            return (
              <Fragment key={token.index}>
                <Box as="span" color="gray.500">
                  {token.tag}:
                </Box>
                <Box as="span" color={`${BookTagColors[token.tag]}.300`}>
                  {token.value}
                </Box>
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
