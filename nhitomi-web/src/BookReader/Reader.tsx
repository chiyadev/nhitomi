import React, { Dispatch, useLayoutEffect, useMemo, useRef, useState } from "react";
import { Book, BookContent } from "nhitomi-api";
import { ImageBase, LayoutEngine, LayoutImage } from "./layoutEngine";
import { useLayout } from "../LayoutManager";
import { CoverImage } from "../Components/CoverImage";
import { useClient, useClientInfo } from "../ClientManager";
import VisibilitySensor from "react-visibility-sensor";
import { useConfig } from "../ConfigManager";
import { ScrollPreserver } from "./ScrollPreserver";
import { ScrollManager } from "./ScrollManager";
import { KeyHandler } from "./KeyHandler";
import { useShortcutPress } from "../shortcut";
import { animated, useSpring } from "react-spring";
import { NonSupporterPageLimit } from "../Support/Limits";
import { PageLimited } from "./PageLimited";

export const Reader = ({
  book,
  content,
  viewportWidth,
}: {
  book: Book;
  content: BookContent;
  viewportWidth?: number;
}) => {
  const { screen, height: viewportHeight } = useLayout();
  const { isSupporter } = useClientInfo();

  const pageCount = useMemo(() => {
    let count = content.pageCount;

    if (!isSupporter) {
      count = Math.min(count, NonSupporterPageLimit);
    }

    return count;
  }, [content, isSupporter]);

  const layoutEngine = useMemo(() => new LayoutEngine(), []);
  const [images, setImages] = useState<(ImageBase | undefined)[]>(() => new Array(pageCount));

  useLayoutEffect(() => layoutEngine.initialize(pageCount), [book, content, layoutEngine]);

  const [imagesPerRow] = useConfig("bookReaderImagesPerRow");
  const [viewportBound] = useConfig("bookReaderViewportBound");
  const [leftToRight] = useConfig("bookReaderLeftToRight");
  const [singleCover] = useConfig("bookReaderSingleCover");

  const layout = useMemo(() => {
    if (!viewportWidth) {
      return;
    }

    return layoutEngine.recompute(images || [], {
      viewportWidth,
      viewportHeight,
      viewportBound,
      leftToRight,
      itemsPerRow: imagesPerRow,
      initialRowLimit: singleCover ? 1 : imagesPerRow,
    });
  }, [images, imagesPerRow, layoutEngine, leftToRight, singleCover, viewportBound, viewportHeight, viewportWidth]);

  const setImage = useMemo(() => {
    const list: Dispatch<ImageBase | undefined>[] = [];

    for (let i = 0; i < pageCount; i++) {
      const index = i;

      list.push((image) => {
        setImages((images) => {
          const array = images.slice();
          array[index] = image;
          return array;
        });
      });
    }

    return list;
  }, [content]);

  let preload: number;

  switch (screen) {
    case "sm":
      preload = viewportHeight * 4;
      break;
    case "lg":
      preload = viewportHeight * 2;
      break;
  }

  const ref = useRef<HTMLDivElement>(null);

  if (!layout) {
    return null;
  }

  return (
    <div
      ref={ref}
      className="relative"
      style={{
        width: layout.width,
        height: layout.height,
      }}
    >
      {useMemo(
        () => (
          <>
            <KeyHandler layout={layout} />
            <ScrollManager containerRef={ref} layout={layout} />
            <ScrollPreserver containerRef={ref} layout={layout} />
          </>
        ),
        [layout]
      )}

      {useMemo(
        () =>
          layout.images.map((image, i) => (
            <PageWrapper
              key={i}
              book={book}
              content={content}
              index={i}
              preload={preload}
              image={image}
              setImage={setImage[i]}
            />
          )),
        [book, content, layout, preload, setImage]
      )}
    </div>
  );
};

const PageWrapper = ({
  book,
  content,
  index,
  preload,
  image,
  setImage,
}: {
  book: Book;
  content: BookContent;
  index: number;
  preload: number;
  image: LayoutImage;
  setImage: Dispatch<ImageBase | undefined>;
}) => {
  const [visible, setVisible] = useState(false);

  const style = useMemo(() => ({ left: image.x, top: image.y, width: image.width, height: image.height }), [image]);

  return useMemo(
    () => (
      <VisibilitySensor
        delayedCall
        partialVisibility
        offset={{ top: -preload, bottom: -preload }}
        onChange={setVisible}
      >
        <div className="absolute" style={style}>
          {visible && <PageContent book={book} content={content} index={index} setImage={setImage} />}
        </div>
      </VisibilitySensor>
    ),
    [preload, visible, style, book, content, index, setImage]
  );
};

const PageContent = ({
  book,
  content,
  index,
  setImage,
}: {
  book: Book;
  content: BookContent;
  index: number;
  setImage: Dispatch<ImageBase | undefined>;
}) => {
  const client = useClient();
  const { isSupporter } = useClientInfo();

  const image = useMemo(
    () => (
      <CoverImage
        cacheKey={`books/${book.id}/contents/${content.id}/pages/${index}`}
        className="w-full h-full"
        sizing="contain"
        onLoad={async () => await client.book.getBookImage({ id: book.id, contentId: content.id, index })}
        onLoaded={setImage}
      />
    ),
    [client.book, book, content, index, setImage]
  );

  const isTruncatedPage =
    !isSupporter && content.pageCount > NonSupporterPageLimit && index + 1 === NonSupporterPageLimit;

  return (
    <>
      {image}

      {isTruncatedPage ? (
        <div className="absolute top-0 left-0 w-full h-full bg-black bg-blur">
          <PageLimited book={book} content={content} />
        </div>
      ) : (
        <PageNumberOverlay index={index} />
      )}
    </>
  );
};

const PageNumberOverlay = ({ index }: { index: number }) => {
  const [shouldShow] = useShortcutPress("bookReaderPageNumberKey");
  const [visible, setVisible] = useState(false);

  const numberStyle = useSpring({
    opacity: shouldShow ? 0.75 : 0,
    fontSize: shouldShow ? 120 : 100,
    onChange: {
      opacity: (v) => setVisible(v > 0),
    },
  });

  if (!visible) {
    return null;
  }

  return (
    <animated.div
      style={numberStyle}
      className="absolute top-0 left-0 w-full h-full bg-black pointer-events-none font-bold flex items-center justify-center"
    >
      <span className="opacity-50">{index + 1}</span>
    </animated.div>
  );
};
