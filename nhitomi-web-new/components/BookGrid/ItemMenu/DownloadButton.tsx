import React, { memo, useMemo, useState } from "react";
import { useT } from "../../../locales";
import BookDownloader from "../../BookDownloader";
import { Icon, MenuItem } from "@chakra-ui/react";
import { FaDownload } from "react-icons/fa";
import { Book, BookContent } from "nhitomi-api";
import ElementPortal from "../../ElementPortal";
import { NonSupporterPageLimit } from "../../../utils/constants";
import { useClientInfoAuth } from "../../../utils/client";

const DownloadButton = ({ book, content }: { book: Book; content: BookContent }) => {
  const info = useClientInfoAuth();
  const t = useT();
  const [open, setOpen] = useState(false);

  const pageLimited = !info?.user.isSupporter && content.pageCount > NonSupporterPageLimit;

  return (
    <>
      <BookDownloader open={open} setOpen={setOpen} targets={useMemo(() => [{ book, content }], [book, content])} />

      <ElementPortal.Consumer>
        <MenuItem icon={<Icon as={FaDownload} />} onClick={() => setOpen(true)} isDisabled={pageLimited}>
          {t("BookGrid.ItemMenu.DownloadButton.text")}
        </MenuItem>
      </ElementPortal.Consumer>
    </>
  );
};

export default memo(DownloadButton);
