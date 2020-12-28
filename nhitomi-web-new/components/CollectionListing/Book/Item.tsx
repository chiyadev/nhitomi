import React, { memo, ReactNode, useMemo, useState } from "react";
import { Book, Collection } from "nhitomi-api";
import { useBookContentSelector } from "../../../utils/book";
import NextLink from "next/link";
import { AspectRatio, chakra, Icon, Link, Menu, MenuButton, MenuList, Text } from "@chakra-ui/react";
import BookImage from "../../BookImage";
import ItemMenu from "./ItemMenu";
import { FaEllipsisV } from "react-icons/fa";

const Item = ({ cover, collection }: { cover?: Book; collection: Collection }) => {
  const selectContent = useBookContentSelector();

  const imageNode = useMemo(
    () => (
      <AspectRatio ratio={13 / 19}>
        {cover ? (
          <BookImage
            book={cover}
            content={selectContent(cover.contents)}
            index={-1}
            intersection={{ rootMargin: "100%" }}
            animateIn="scale"
            objectFit="cover"
            objectPosition="center"
          />
        ) : (
          <div />
        )}
      </AspectRatio>
    ),
    [cover, selectContent]
  );

  const overlayNode = useMemo(
    () => (
      <chakra.div position="absolute" bottom={0} left={0} w="full" bg="white" color="black" p={1} opacity={0.9}>
        <Text fontWeight="bold">{collection.name}</Text>

        {collection.description && <Text fontSize="sm">{collection.description}</Text>}
      </chakra.div>
    ),
    [collection]
  );

  const [menu, setMenu] = useState<ReactNode>();

  const menuNode = useMemo(
    () => (
      <chakra.div position="absolute" top={1} right={1}>
        <Menu>
          <Link as={MenuButton} p={1} borderRadius="md">
            <Icon as={FaEllipsisV} fontSize="lg" opacity={0.9} />
          </Link>

          <MenuList>{menu}</MenuList>
        </Menu>
      </chakra.div>
    ),
    [menu]
  );

  return (
    <>
      <chakra.div position="relative">
        <NextLink href={`/collections/${collection.id}`} passHref>
          <Link position="relative" display="block" borderRadius="md" overflow="hidden">
            {imageNode}
            {overlayNode}
          </Link>
        </NextLink>

        {menuNode}
      </chakra.div>

      <ItemMenu collection={collection} setMenu={setMenu} />
    </>
  );
};

export default memo(Item);
