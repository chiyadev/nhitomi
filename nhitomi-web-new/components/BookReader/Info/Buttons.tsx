import React, { memo, useMemo, useState } from "react";
import { Button, Icon, LightMode, Wrap, WrapItem } from "@chakra-ui/react";
import { FaDownload, FaHeart, FaPlus } from "react-icons/fa";
import BookDownloader from "../../BookDownloader";
import { Book, BookContent } from "nhitomi-api";
import { useT } from "../../../locales";

const Buttons = ({ book, content }: { book: Book; content: BookContent }) => {
  const t = useT();
  const [download, setDownload] = useState(false);

  return (
    <>
      <BookDownloader
        targets={useMemo(() => [{ book, content }], [book, content])}
        open={download}
        setOpen={setDownload}
      />

      <Wrap spacing={2}>
        <WrapItem>
          <LightMode>
            <Button colorScheme="red" leftIcon={<Icon as={FaHeart} />}>
              {t("BookReader.Info.Buttons.favorite")}
            </Button>
          </LightMode>
        </WrapItem>

        <WrapItem>
          <Button leftIcon={<Icon as={FaPlus} />}>{t("BookReader.Info.Buttons.addToCollection")}</Button>
        </WrapItem>

        <WrapItem>
          <Button leftIcon={<Icon as={FaDownload} />} onClick={() => setDownload(true)}>
            {t("BookReader.Info.Buttons.download")}
          </Button>
        </WrapItem>
      </Wrap>
    </>
  );
};

export default memo(Buttons);
