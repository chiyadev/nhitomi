import React from "react";
import { useShortcut } from "../shortcut";
import { FormattedMessage } from "react-intl";
import { useAlert } from "../NotificationManager";
import { useConfig } from "../ConfigManager";
import { CurrentPage } from "./ScrollManager";
import { usePageState } from "../state";
import { LayoutResult } from "./layoutEngine";

export const KeyHandler = ({ layout }: { layout: LayoutResult }) => {
  const { alert } = useAlert();

  // config keys
  const [imagesPerRow, setImagesPerRow] = useConfig("bookReaderImagesPerRow");
  const [viewportBound, setViewportBound] = useConfig("bookReaderViewportBound");
  const [leftToRight, setLeftToRight] = useConfig("bookReaderLeftToRight");
  const [singleCover, setSingleCover] = useConfig("bookReaderSingleCover");

  useShortcut("bookReaderImagesPerRowKey", () => {
    const v = imagesPerRow === 1 ? 2 : 1;
    setImagesPerRow(v);
    alert(<FormattedMessage id='pages.bookReader.alerts.imagesPerRow' values={{ value: v }} />, "info");
  });

  useShortcut("bookReaderViewportBoundKey", () => {
    const v = !viewportBound;
    setViewportBound(v);
    alert(<FormattedMessage id='pages.bookReader.alerts.viewportBound' values={{ value: v }} />, "info");
  });

  useShortcut("bookReaderLeftToRightKey", () => {
    const v = !leftToRight;
    setLeftToRight(v);
    alert(<FormattedMessage id='pages.bookReader.alerts.leftToRight' values={{ value: v }} />, "info");
  });

  useShortcut("bookReaderSingleCoverKey", () => {
    const v = !singleCover;
    setSingleCover(v);
    alert(<FormattedMessage id='pages.bookReader.alerts.singleCover' values={{ value: v }} />, "info");
  });

  // scroll keys
  const [current, setCurrent] = usePageState<CurrentPage>("page", { rowPassive: 0, pagePassive: 0 });

  useShortcut("bookReaderFirstPageKey", () => setCurrent({ ...current, rowInduced: 0 }));
  useShortcut("bookReaderLastPageKey", () => setCurrent({ ...current, rowInduced: layout.rows.length - 1 }));
  useShortcut("bookReaderPreviousPageKey", () => setCurrent({ ...current, rowInduced: current.rowPassive - 1 }));
  useShortcut("bookReaderNextPageKey", () => setCurrent({ ...current, rowInduced: current.rowPassive + 1 }));

  return null;
};
