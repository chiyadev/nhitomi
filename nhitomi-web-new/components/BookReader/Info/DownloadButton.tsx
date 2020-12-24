import React, { memo, useMemo, useState } from "react";
import BookDownloader from "../../BookDownloader";
import { Book, BookContent } from "nhitomi-api";
import { Button, Icon } from "@chakra-ui/react";
import { FaDownload } from "react-icons/fa";
import { useT } from "../../../locales";

const DownloadButton = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();
  const [open, setOpen] = useState(false);

  return (
    <>
      <BookDownloader open={open} setOpen={setOpen} targets={useMemo(() => [{ book, content }], [book, content])} />

      <Button leftIcon={<Icon as={FaDownload} />} onClick={() => setOpen(true)}>
        {t("BookReader.Info.DownloadButton.text")}
      </Button>
    </>
  );
};

export default memo(DownloadButton);
