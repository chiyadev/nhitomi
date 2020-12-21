import React, { memo, useMemo, useState } from "react";
import { chakra, HStack, Kbd, Spacer, Tag } from "@chakra-ui/react";
import { BookTag } from "nhitomi-api";
import { BookTagColors } from "../../../../utils/constants";
import { useT } from "../../../../locales";

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
  const t = useT();

  return (
    <HStack
      position="relative"
      ml={-1000}
      pl={1000}
      mr={-1000}
      pr={1000}
      pt={focused ? 1 : 0}
      pb={focused ? 1 : 0}
      bg={focused || hover ? "gray.600" : undefined}
      transition="all .2s"
      cursor="pointer"
      onClick={onClick}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
    >
      <div>
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
      </div>

      <Tag colorScheme={BookTagColors[tag]} ml={2} transition="all .2s" opacity={focused || hover ? 1 : 0}>
        {t("BookTag", { value: tag })}
      </Tag>

      <Spacer />

      <Kbd fontSize={10} transition="all .2s" opacity={focused ? 1 : 0}>
        enter ↵
      </Kbd>
    </HStack>
  );
};

export default memo(TagItem);
