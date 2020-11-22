import React, { memo, useMemo, useState } from "react";
import { Box, chakra, Flex, Kbd, Spacer } from "@chakra-ui/react";
import { BookTag } from "nhitomi-api";
import { BookTagColors } from "../../../../utils/colors";

const TagItem = ({
  focused,
  onClick,
  comparand,
  text,
  tag,
}: {
  focused: boolean;
  onClick?: () => void;
  comparand: string;
  text: string;
  tag: BookTag;
}) => {
  const [hover, setHover] = useState(false);

  return (
    <Flex
      position="relative"
      ml={-1000}
      pl={1000}
      mr={-1000}
      pr={1000}
      bg={focused || hover ? "gray.600" : undefined}
      transition="all .2s"
      cursor="pointer"
      onClick={onClick}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
    >
      <Box>
        {useMemo(
          () =>
            text.split("").map((char, i) => (
              <chakra.span
                key={i}
                color={comparand.includes(char) ? undefined : "gray.500"}
                //fontWeight={comparand.includes(char) ? "bold" : undefined}
              >
                {char}
              </chakra.span>
            )),
          [comparand, text]
        )}

        <chakra.span
          ml={2}
          fontSize={12}
          transition="all .2s"
          opacity={focused || hover ? 0.75 : 0}
          color={`${BookTagColors[tag]}.300`}
        >
          {tag}
        </chakra.span>
      </Box>

      <Spacer />

      <Flex direction="column" justify="center">
        <Kbd fontSize={10} transition="all .2s" opacity={focused ? 1 : 0}>
          enter ↵
        </Kbd>
      </Flex>
    </Flex>
  );
};

export default memo(TagItem);
