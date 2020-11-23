import React, { Dispatch, memo, Ref, useEffect, useRef, useState } from "react";
import { Box, Flex, Icon, Input as InputCore, Spinner, Square } from "@chakra-ui/react";
import { FaSearch } from "react-icons/fa";
import mergeRefs from "react-merge-refs";
import Highlighter from "./Highlighter";

const Input = ({
  inputRef,
  value,
  setValue,
  loading,
  onSubmit,
  onSuggestChange,
  onFocus,
}: {
  inputRef: Ref<HTMLInputElement>;
  value: string;
  setValue: Dispatch<string>;
  loading: boolean;
  onSubmit?: (force?: boolean) => void;
  onSuggestChange?: Dispatch<number>;
  onFocus?: Dispatch<boolean>;
}) => {
  const ref = useRef<HTMLInputElement>(null);
  const [offset, setOffset] = useState(0);

  useEffect(() => {
    const input = ref.current;
    if (!input) return;

    const handler = () => setOffset(-input.scrollLeft);

    input.addEventListener("scroll", handler);
    return () => input.removeEventListener("scroll", handler);
  }, [inputRef]);

  return (
    <Box position="relative">
      <InputCore
        ref={mergeRefs([ref, inputRef])}
        autoFocus
        value={value}
        onChange={({ target: { value } }) => setValue(value)}
        placeholder="Search anything on nhitomi..."
        size="lg"
        pl={12}
        fontSize="md"
        bg="none"
        color="transparent"
        css={{
          caretColor: "white",
        }}
        onFocus={() => onFocus?.(true)}
        onBlur={() => onFocus?.(false)}
        onKeyDown={(e) => {
          let handled = true;

          switch (e.keyCode) {
            // enter
            case 13:
              onSubmit?.();
              break;

            // up
            case 38:
              onSuggestChange?.(-1);
              break;

            // down
            case 40:
              onSuggestChange?.(1);
              break;

            default:
              handled = false;
              break;
          }

          if (handled) {
            e.preventDefault();
          }
        }}
      />

      <Square
        position="absolute"
        top={0}
        p={4}
        ml="1px"
        cursor={loading ? undefined : "pointer"}
        zIndex={1}
        onClick={() => {
          !loading && onSubmit?.(true);
        }}
      >
        <Icon as={loading ? Spinner : FaSearch} />
      </Square>

      <Flex
        position="absolute"
        top={0}
        ml="1px"
        pl={12}
        w="full"
        h="full"
        zIndex={-1}
        direction="column"
        justify="center"
      >
        <Box overflow="hidden" w="full">
          <Box style={{ marginLeft: offset }}>
            <Highlighter text={value} />
          </Box>
        </Box>
      </Flex>
    </Box>
  );
};

export default memo(Input);
