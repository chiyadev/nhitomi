import { memo } from "react";
import { useReaderScroll } from "../../scroll";
import { useHotkeys } from "react-hotkeys-hook";
import { LayoutResult } from "./layoutEngine";
import { useConfig } from "../../../../utils/config";

const KeyHandler = ({ layout }: { layout: LayoutResult }) => {
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
    "right, d",
    (e) => {
      e.preventDefault();
      setScroll((scroll) => ({ ...scroll, inducedRow: scroll.currentRow - 1 }));
    },
    [setScroll]
  );

  useHotkeys(
    "left, a",
    (e) => {
      e.preventDefault();
      setScroll((scroll) => ({ ...scroll, inducedRow: scroll.currentRow + 1 }));
    },
    [setScroll]
  );

  const [, setViewportBound] = useConfig("bookViewportBound");
  const [, setLeftToRight] = useConfig("bookLeftToRight");
  const [, setImagesPerRow] = useConfig("bookImagesPerRow");
  const [, setSingleCover] = useConfig("bookSingleCover");

  useHotkeys(
    "c",
    (e) => {
      e.preventDefault();
      setViewportBound((value) => !value);
    },
    [setViewportBound]
  );

  useHotkeys(
    "l",
    (e) => {
      e.preventDefault();
      setLeftToRight((value) => !value);
    },
    [setLeftToRight]
  );

  useHotkeys(
    "x",
    (e) => {
      e.preventDefault();
      setImagesPerRow((value) => (value === 1 ? 2 : 1));
    },
    [setImagesPerRow]
  );

  useHotkeys(
    "k",
    (e) => {
      e.preventDefault();
      setSingleCover((value) => !value);
    },
    [setSingleCover]
  );

  return null;
};

export default memo(KeyHandler);
