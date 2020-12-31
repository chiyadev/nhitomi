import React, { Dispatch, memo, useState } from "react";
import { Collection } from "nhitomi-api";
import BookDownloader, { DownloadTarget } from "../../BookDownloader";
import CollectionItemSelector from "../../CollectionItemSelector/Book";
import { useBookContentSelector } from "../../../utils/book";

const CollectionDownloader = ({
  open,
  setOpen,
  collection,
}: {
  open: boolean;
  setOpen: Dispatch<boolean>;
  collection: Collection;
}) => {
  const selectContent = useBookContentSelector();

  const [download, setDownload] = useState(false);
  const [targets, setTargets] = useState<DownloadTarget[]>([]);

  return (
    <>
      <CollectionItemSelector
        open={open}
        setOpen={setOpen}
        collection={collection}
        onSelect={(items) => {
          setOpen(false);
          setDownload(true);
          setTargets(items.map((book) => ({ book, content: selectContent(book.contents) })));
        }}
      />

      <BookDownloader open={download} setOpen={setDownload} targets={targets} />
    </>
  );
};

export default memo(CollectionDownloader);
