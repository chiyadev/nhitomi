import React, { Dispatch, memo, useState } from "react";
import { Collection } from "nhitomi-api";
import { chakra, HStack, Link, VStack, Icon, Fade } from "@chakra-ui/react";
import { FaExternalLinkAlt } from "react-icons/fa";

const ItemDisplay = ({
  collection,
  disabled,
  onSelect,
}: {
  collection: Collection;
  disabled: boolean;
  onSelect?: Dispatch<Collection>;
}) => {
  const [hover, setHover] = useState(false);

  return (
    <VStack
      align="stretch"
      spacing={0}
      opacity={disabled ? 0.5 : 1}
      transition="all .2s ease-out"
      onMouseEnter={() => setHover(!disabled && true)}
      onMouseLeave={() => setHover(!disabled && false)}
    >
      <HStack spacing={2}>
        <Link as="button" disabled={disabled} onClick={() => onSelect?.(collection)}>
          {collection.name}
        </Link>

        <Fade in={hover} unmountOnExit>
          <Link color="blue.300" fontSize="xs" href={`/collections/${collection.id}`} isExternal>
            <Icon as={FaExternalLinkAlt} />
          </Link>
        </Fade>
      </HStack>

      {collection.description && (
        <chakra.div color="gray.500" fontSize="sm">
          {collection.description}
        </chakra.div>
      )}
    </VStack>
  );
};

export default memo(ItemDisplay);
