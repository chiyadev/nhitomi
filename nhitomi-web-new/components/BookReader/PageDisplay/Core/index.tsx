import React, { Dispatch, memo, SetStateAction, useMemo, useRef, useState } from "react";
import { chakra, useBreakpointValue } from "@chakra-ui/react";
import Page from "./Page";
import { Book, BookContent } from "nhitomi-api";
import { ImageInfo, LayoutEngine } from "./layoutEngine";
import { useWindowSize } from "../../../../utils/hooks";
import { useConfig } from "../../../../utils/config";
import ScrollPreserver from "./ScrollPreserver";
import ScrollManager from "./ScrollManager";
import KeyHandler from "./KeyHandler";

const PageDisplay = ({ book, content }: { book: Book; content: BookContent }) => {
  const pageCount = useMemo(() => {
    return content.pageCount;
  }, [content]);

  const layoutEngine = useMemo(() => new LayoutEngine(pageCount), []);
  const [images, setImages] = useState<(ImageInfo | undefined)[]>(() => new Array(pageCount));
  const [viewportWidth, viewportHeight] = useWindowSize();
  const [viewportBoundCore] = useConfig("bookViewportBound");
  const [leftToRight] = useConfig("bookLeftToRight");
  const [imagesPerRowCore] = useConfig("bookImagesPerRow");
  const [singleCover] = useConfig("bookSingleCover");

  const screen = useBreakpointValue({ base: "sm", md: "md" });
  const viewportBound = screen === "sm" ? false : viewportBoundCore;
  const imagesPerRow = screen === "sm" ? 1 : imagesPerRowCore;

  const layout = useMemo(() => {
    return layoutEngine.recompute(images || [], {
      viewportWidth,
      viewportHeight,
      viewportBound,
      leftToRight,
      itemsPerRow: imagesPerRow,
      initialRowLimit: singleCover ? 1 : imagesPerRow,
    });
  }, [layoutEngine, images, viewportWidth, viewportHeight, viewportBound, leftToRight, imagesPerRow, singleCover]);

  const imageSetters = useMemo(() => {
    const result: Dispatch<SetStateAction<ImageInfo | undefined>>[] = [];

    for (let i = 0; i < pageCount; i++) {
      const index = i;

      result.push((image) => {
        setImages((images) => {
          const array = images.slice();

          if (typeof image === "function") {
            array[index] = image(array[index]);
          } else {
            array[index] = image;
          }

          return array;
        });
      });
    }

    return result;
  }, []);

  const ref = useRef<HTMLDivElement>(null);

  return (
    <chakra.div
      ref={ref}
      position="relative"
      overflow="hidden"
      style={useMemo(
        () => ({
          width: layout.width,
          height: layout.height,
        }),
        [layout.width, layout.height]
      )}
    >
      <KeyHandler layout={layout} />
      <ScrollPreserver containerRef={ref} layout={layout} />
      <ScrollManager containerRef={ref} layout={layout} />

      {layout.images.map((image, i) => (
        <Page key={i} book={book} content={content} index={i} image={image} setImage={imageSetters[i]} />
      ))}
    </chakra.div>
  );
};

export default memo(PageDisplay);
