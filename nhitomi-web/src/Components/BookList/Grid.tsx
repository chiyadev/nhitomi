import React, { ReactNode, useMemo, useState } from "react";
import { getBreakpoint, LargeBreakpoints, ScreenBreakpoint, SmallBreakpoints, useLayout } from "../../LayoutManager";
import { css, cx } from "emotion";
import { CoverImage } from "../CoverImage";
import { useClient } from "../../ClientManager";
import { animated, useSpring } from "react-spring";
import { BookReaderLink } from "../../BookReader";
import VisibilitySensor from "react-visibility-sensor";
import { BookListItem, useBookList, useContentSelector } from ".";
import { BookContent } from "nhitomi-api";
import { useConfig } from "../../ConfigManager";
import { ContextMenu } from "../ContextMenu";
import { Overlay } from "./Overlay";

export const Grid = ({ width, menu, empty }: { width: number; menu?: ReactNode; empty?: ReactNode }) => {
  const { items } = useBookList();

  const { spacing, rowWidth, itemWidth, itemHeight } = useMemo(() => {
    let spacing: number;
    let rowItems: number;
    let rowWidth: number;

    if (width < ScreenBreakpoint) {
      const breakpoint = getBreakpoint(SmallBreakpoints, width) || 0;

      spacing = 4;
      rowItems = SmallBreakpoints.indexOf(breakpoint) + 2;
      rowWidth = width;
    } else {
      const breakpoint = getBreakpoint(LargeBreakpoints, width) || 0;

      spacing = 6;
      rowItems = LargeBreakpoints.indexOf(breakpoint) + 3;
      rowWidth = breakpoint;
    }

    const itemWidth = (rowWidth - spacing * (rowItems + 1)) / rowItems;
    const itemHeight = (itemWidth * 7) / 5;

    return { spacing, rowItems, rowWidth, itemWidth, itemHeight };
  }, [width]);

  const { screen, height } = useLayout();
  let preload: number;

  switch (screen) {
    case "sm":
      preload = height * 2;
      break;
    case "lg":
      preload = 400;
      break;
  }

  const itemSpacingClass = useMemo(
    () => css`
      margin: ${spacing / 2}px;
    `,
    [spacing]
  );

  const itemsMemoized = useMemo(
    () =>
      items.map((item) => (
        <ItemWrapper
          key={item.id}
          book={item}
          preload={preload}
          width={itemWidth}
          height={itemHeight}
          className={itemSpacingClass}
        />
      )),
    [items, preload, itemWidth, itemHeight, itemSpacingClass]
  );

  return (
    <div style={{ maxWidth: rowWidth }} className="mx-auto w-full">
      <MenuWrapper>{menu}</MenuWrapper>

      {useMemo(
        () => (
          <div
            className={cx(
              "flex flex-row flex-wrap justify-center",
              css`
                padding-left: ${spacing / 2}px;
                padding-right: ${spacing / 2}px;
              `
            )}
          >
            {itemsMemoized}
            {items.length ? null : empty}
          </div>
        ),
        [empty, itemsMemoized, items]
      )}
    </div>
  );
};

const MenuWrapper = ({ children }: { children?: ReactNode }) => {
  const style = useSpring({
    from: { opacity: 0 },
    to: { opacity: 1 },
  });

  return (
    <>
      {children && (
        <animated.div style={style} className="w-full flex flex-row justify-end px-2 mb-2">
          {children}
        </animated.div>
      )}
    </>
  );
};

const ItemWrapper = ({
  book,
  preload,
  width,
  height,
  className,
}: {
  book: BookListItem;
  preload: number;
  width: number;
  height: number;
  className?: string;
}) => {
  const [visible, setVisible] = useState(false);

  const inner = useMemo(() => <ItemContent book={book} width={width} height={height} className={className} />, [
    book,
    width,
    height,
    className,
  ]);

  const placeholder = useMemo(() => <div style={{ width, height }} className={className} />, [
    width,
    height,
    className,
  ]);

  return useMemo(
    () => (
      <VisibilitySensor
        delayedCall
        partialVisibility
        offset={{ top: -preload, bottom: -preload }}
        onChange={setVisible}
      >
        {visible ? inner : placeholder}
      </VisibilitySensor>
    ),
    [visible, inner, placeholder]
  );
};

const ItemContent = ({
  book,
  width,
  height,
  className,
}: {
  book: BookListItem;
  width: number;
  height: number;
  className?: string;
}) => {
  const contentSelector = useContentSelector();
  const { LinkComponent } = useBookList();
  const [hover, setHover] = useState(false);

  const content = useMemo(() => contentSelector(book.contents), [book.contents, contentSelector]);
  const contextOverlay = useMemo(() => <Overlay book={book} content={content} />, [book, content]);
  const hoverOverlay = useMemo(() => <ItemOverlay book={book} hover={hover} />, [book, hover]);
  const image = useMemo(() => <ItemCover book={book} content={content} />, [book, content]);

  const children = useMemo(
    () => (
      <>
        {image}
        {hoverOverlay}
      </>
    ),
    [image, hoverOverlay]
  );

  return (
    <div
      style={{ width, height }}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      className={cx("rounded overflow-hidden relative", className)}
    >
      <ContextMenu overlay={contextOverlay}>
        {LinkComponent ? (
          <LinkComponent id={book.id} contentId={content?.id}>
            {children}
          </LinkComponent>
        ) : content ? (
          <BookReaderLink id={book.id} contentId={content.id}>
            {children}
          </BookReaderLink>
        ) : (
          children
        )}
      </ContextMenu>
    </div>
  );
};

const ItemCover = ({ book, content }: { book: BookListItem; content?: BookContent }) => {
  const client = useClient();
  const { getCoverRequest } = useBookList();

  return useMemo(
    () =>
      content ? (
        <CoverImage
          key={`${book.id}/${content.id}`}
          cacheKey={`books/${book.id}/contents/${content.id}/pages/-1`}
          zoomIn
          className="w-full h-full rounded overflow-hidden"
          onLoad={async () =>
            await client.book.getBookImage(
              getCoverRequest?.(book, content) || {
                id: book.id,
                contentId: content.id,
                index: -1,
              }
            )
          }
        />
      ) : (
        <div className="w-full h-full" />
      ),
    [book, client.book, content, getCoverRequest]
  );
};

const ItemOverlay = ({ book, hover }: { book: BookListItem; hover?: boolean }) => {
  let [preferEnglishName] = useConfig("bookReaderPreferEnglishName");

  const { overlayVisible, preferEnglishName: preferEnglishNameOverride } = useBookList();
  hover = overlayVisible || hover;

  if (typeof preferEnglishNameOverride !== "undefined") preferEnglishName = preferEnglishNameOverride;

  const [visible, setVisible] = useState(hover);
  const style = useSpring({
    from: {
      opacity: 0,
    },
    opacity: hover ? 1 : 0,
    marginBottom: hover ? 0 : -5,
    onChange: {
      opacity: (v) => setVisible(v > 0),
    },
  });

  const inner = useMemo(
    () =>
      visible && (
        <div className="p-1 bg-white bg-blur text-black rounded-b">
          <span className="block truncate font-bold">
            {(preferEnglishName && book.englishName) || book.primaryName}
          </span>

          {book.englishName && book.primaryName !== book.englishName && (
            <span className="block text-sm truncate">
              {(!preferEnglishName && book.englishName) || book.primaryName}
            </span>
          )}
        </div>
      ),
    [book.englishName, book.primaryName, preferEnglishName, visible]
  );

  if (!visible) return null;

  return (
    <animated.div style={style} className="absolute bottom-0 left-0 w-full">
      {inner}
    </animated.div>
  );
};
