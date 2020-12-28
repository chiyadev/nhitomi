import React, { memo, ReactNode, useMemo, useRef, useState } from "react";
import { Book } from "nhitomi-api";
import { AspectRatio, chakra, Fade, Icon, Link, Menu, MenuButton, MenuList, SlideFade, Text } from "@chakra-ui/react";
import BookImage from "../BookImage";
import NextLink from "next/link";
import { useBookContent } from "../../utils/book";
import { useConfig } from "../../utils/config";
import ItemMenu from "./ItemMenu";
import { FaEllipsisV } from "react-icons/fa";

const Item = ({ book }: { book: Book }) => {
  const linkRef = useRef<HTMLAnchorElement>(null);
  const content = useBookContent(book);

  const [forceOverlay] = useConfig("bookForceInfoOverlay");
  const [hover, setHover] = useState(false);
  const [focus, setFocus] = useState(false);
  const showOverlay = forceOverlay || hover || focus;

  const imageNode = useMemo(
    () => (
      <AspectRatio ratio={13 / 19}>
        <BookImage
          book={book}
          content={content}
          index={-1}
          intersection={{ rootMargin: "100%" }}
          animateIn="scale"
          objectFit="cover"
          objectPosition="center"
        />
      </AspectRatio>
    ),
    [book, content]
  );

  const overlayNode = useMemo(
    () => (
      <SlideFade in={showOverlay} unmountOnExit>
        <chakra.div position="absolute" bottom={0} left={0} right={0} bg="white" color="black" p={1} opacity={0.9}>
          <Text fontWeight="bold">{book.primaryName}</Text>

          {book.primaryName !== book.englishName && <Text fontSize="sm">{book.englishName}</Text>}
        </chakra.div>
      </SlideFade>
    ),
    [showOverlay, book]
  );

  const [menu, setMenu] = useState<ReactNode>();

  const menuNode = useMemo(
    () => (
      <Fade in={showOverlay} unmountOnExit>
        <chakra.div position="absolute" top={1} right={1}>
          <Menu>
            <Link as={MenuButton} p={1} borderRadius="md">
              <Icon as={FaEllipsisV} fontSize="lg" opacity={0.9} />
            </Link>

            <MenuList>{menu}</MenuList>
          </Menu>
        </chakra.div>
      </Fade>
    ),
    [showOverlay, menu]
  );

  return (
    <>
      <chakra.div
        position="relative"
        onMouseEnter={() => setHover(true)}
        onMouseLeave={() => setHover(false)}
        onMouseMove={() => setHover(true)}
        onTouchStart={() => linkRef.current?.focus({ preventScroll: true })}
        onFocus={() => setFocus(true)}
        onBlur={() => setFocus(false)}
      >
        <NextLink href={`/books/${book.id}/contents/${content.id}`} passHref>
          <Link ref={linkRef} position="relative" display="block" borderRadius="md" overflow="hidden">
            {imageNode}
            {overlayNode}
          </Link>
        </NextLink>

        {menuNode}
      </chakra.div>

      <ItemMenu book={book} content={content} setMenu={setMenu} />
    </>
  );
};

export default memo(Item);
