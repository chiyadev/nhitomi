import React, { memo, useMemo, useState } from "react";
import { useT } from "../../../locales";
import BookDownloader from "../../BookDownloader";
import { Icon, MenuItem } from "@chakra-ui/react";
import { FaDownload } from "react-icons/fa";
import { Book, BookContent } from "nhitomi-api";
import ElementPortal from "../../ElementPortal";

const DownloadButton = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();
  const [open, setOpen] = useState(false);

  return (
    <>
      <BookDownloader open={open} setOpen={setOpen} targets={useMemo(() => [{ book, content }], [book, content])} />

      <ElementPortal.Consumer>
        <MenuItem icon={<Icon as={FaDownload} />} onClick={() => setOpen(true)}>
          {t("BookGrid.ItemMenu.DownloadButton.text")}
        </MenuItem>
      </ElementPortal.Consumer>
    </>
  );
};

export default memo(DownloadButton);
