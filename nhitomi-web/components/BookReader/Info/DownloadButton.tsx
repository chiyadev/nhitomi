import React, { memo, useMemo, useState } from "react";
import BookDownloader from "../../BookDownloader";
import { Book, BookContent } from "nhitomi-api";
import { Button, Icon, Tooltip } from "@chakra-ui/react";
import { FaDownload } from "react-icons/fa";
import { useT } from "../../../locales";
import { useClientInfoAuth } from "../../../utils/client";
import { NonSupporterPageLimit } from "../../../utils/constants";
import NextLink from "next/link";
import { trackEvent } from "../../../utils/umami";

const DownloadButton = ({ book, content }: { book: Book; content: BookContent }) => {
  const info = useClientInfoAuth();
  const t = useT();
  const [open, setOpen] = useState(false);

  const pageLimited = !info?.user.isSupporter && content.pageCount > NonSupporterPageLimit;

  return (
    <>
      <BookDownloader open={open} setOpen={setOpen} targets={useMemo(() => [{ book, content }], [book, content])} />

      {pageLimited ? (
        <Tooltip label={t("BookReader.Info.DownloadButton.limited")}>
          <span>
            <NextLink href="/support" passHref>
              <Button as="a" leftIcon={<Icon as={FaDownload} />}>
                {t("BookReader.Info.DownloadButton.text")}
              </Button>
            </NextLink>
          </span>
        </Tooltip>
      ) : (
        <Button
          leftIcon={<Icon as={FaDownload} />}
          onClick={() => {
            setOpen(true);
            trackEvent("bookReader", "download");
          }}
        >
          {t("BookReader.Info.DownloadButton.text")}
        </Button>
      )}
    </>
  );
};

export default memo(DownloadButton);
