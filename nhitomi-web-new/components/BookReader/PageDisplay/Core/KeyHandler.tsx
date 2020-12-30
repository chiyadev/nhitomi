import React, { memo, useCallback, useRef, useState } from "react";
import { useReaderScroll } from "../../scroll";
import { useHotkeys } from "react-hotkeys-hook";
import { LayoutResult } from "./layoutEngine";
import { useConfig } from "../../../../utils/config";
import { chakra, ScaleFade, VStack } from "@chakra-ui/react";
import ImagesPerRow1 from "../../../../assets/Settings/ImagesPerRow1.png";
import ImagesPerRow2 from "../../../../assets/Settings/ImagesPerRow2.png";
import LeftToRightOn from "../../../../assets/Settings/LeftToRightOn.png";
import LeftToRightOff from "../../../../assets/Settings/LeftToRightOff.png";
import SingleCoverOn from "../../../../assets/Settings/SingleCoverOn.png";
import SingleCoverOff from "../../../../assets/Settings/SingleCoverOff.png";
import ViewportBoundOn from "../../../../assets/Settings/ViewportBoundOn.png";
import ViewportBoundOff from "../../../../assets/Settings/ViewportBoundOff.png";
import { useT } from "../../../../locales";
import { trackEvent } from "../../../../utils/umami";

const KeyHandler = ({ layout }: { layout: LayoutResult }) => {
  const t = useT();
  const [, setScroll] = useReaderScroll();

  useHotkeys(
    "home",
    (e) => {
      e.preventDefault();
      setScroll((scroll) => ({ ...scroll, inducedRow: 0 }));
    },
    [setScroll]
  );

  useHotkeys(
    "end",
    (e) => {
      e.preventDefault();
      setScroll((scroll) => ({ ...scroll, inducedRow: layout.rows.length - 1 }));
    },
    [setScroll, layout]
  );

  useHotkeys(
    "right, d, pageUp",
    (e) => {
      e.preventDefault();
      setScroll((scroll) => ({ ...scroll, inducedRow: scroll.currentRow - 1 }));
    },
    [setScroll]
  );

  useHotkeys(
    "left, a, pageDown",
    (e) => {
      e.preventDefault();
      setScroll((scroll) => ({ ...scroll, inducedRow: scroll.currentRow + 1 }));
    },
    [setScroll]
  );

  const [overlay, setOverlay] = useState(false);
  const overlayTimeout = useRef<number>();

  const [status, setStatus] = useState<{
    key: "viewportBound" | "leftToRight" | "imagesPerRow" | "singleCover";
    img: string;
  }>();

  const showOverlay = useCallback((value: typeof status) => {
    setOverlay(true);
    setStatus(value);

    clearTimeout(overlayTimeout.current);
    overlayTimeout.current = window.setTimeout(() => setOverlay(false), 2000);
  }, []);

  const [, setViewportBound] = useConfig("bookViewportBound");
  const [, setLeftToRight] = useConfig("bookLeftToRight");
  const [, setImagesPerRow] = useConfig("bookImagesPerRow");
  const [, setSingleCover] = useConfig("bookSingleCover");

  useHotkeys(
    "c",
    (e) => {
      e.preventDefault();
      setViewportBound((value) => {
        showOverlay({ key: "viewportBound", img: value ? ViewportBoundOff : ViewportBoundOn });
        return !value;
      });

      trackEvent("bookReader", "viewportBound");
    },
    [showOverlay, setViewportBound]
  );

  useHotkeys(
    "l",
    (e) => {
      e.preventDefault();
      setOverlay(true);
      setLeftToRight((value) => {
        showOverlay({ key: "leftToRight", img: value ? LeftToRightOff : LeftToRightOn });
        return !value;
      });

      trackEvent("bookReader", "leftToRight");
    },
    [showOverlay, setLeftToRight]
  );

  useHotkeys(
    "x",
    (e) => {
      e.preventDefault();
      setOverlay(true);
      setImagesPerRow((value) => {
        showOverlay({ key: "imagesPerRow", img: value === 1 ? ImagesPerRow2 : ImagesPerRow1 });
        return value === 1 ? 2 : 1;
      });

      trackEvent("bookReader", "imagesPerRow");
    },
    [showOverlay, setImagesPerRow]
  );

  useHotkeys(
    "k",
    (e) => {
      e.preventDefault();
      setOverlay(true);
      setSingleCover((value) => {
        showOverlay({ key: "singleCover", img: value ? SingleCoverOff : SingleCoverOn });
        return !value;
      });

      trackEvent("bookReader", "singleCover");
    },
    [showOverlay, setSingleCover]
  );

  if (!status) {
    return null;
  }

  return (
    <chakra.div position="fixed" zIndex="overlay" top="50%" left="50%" transform="translate(-50%, -50%)">
      <ScaleFade in={overlay}>
        <VStack spacing={4} p={4} boxShadow="md" borderRadius="md" bg="gray.800" color="white">
          <chakra.img src={status.img} w={44} />
          <div>{t(`BookReader.PageDisplay.KeyHandler.${status.key}`)}</div>
        </VStack>
      </ScaleFade>
    </chakra.div>
  );
};

export default memo(KeyHandler);
