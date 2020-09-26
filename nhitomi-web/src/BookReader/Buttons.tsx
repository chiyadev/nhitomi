import React, { useState } from "react";
import { useClientInfo, useClientUtils } from "../ClientManager";
import { useNotify } from "../NotificationManager";
import { Book, BookContent, ObjectType, SpecialCollection } from "nhitomi-api";
import { PrefetchResult } from ".";
import { FilledButton } from "../Components/FilledButton";
import { CloudDownloadOutlined, HeartFilled, PlusOutlined } from "@ant-design/icons";
import { getColor } from "../theme";
import { FormattedMessage } from "react-intl";
import { Dropdown } from "../Components/Dropdown";
import { CollectionAddBookDropdownMenu } from "../Components/CollectionAddBookDropdownMenu";
import { useProgress } from "../ProgressManager";
import { Disableable } from "../Components/Disableable";
import { useDownloads } from "../DownloadManager";
import { trackEvent } from "../umami";
import { NonSupporterPageLimit } from "../Support/Limits";
import { Tooltip } from "../Components/Tooltip";
import { SupportLink } from "../Support";

export const Buttons = ({ book, content }: PrefetchResult) => {
  return (
    <div className="flex flex-row flex-wrap -m-1">
      <FavoriteButton book={book} />
      <CollectionAddButton book={book} />
      <DownloadButton book={book} content={content} />
    </div>
  );
};

const FavoriteButton = ({ book }: { book: Book }) => {
  const { addToSpecialCollection } = useClientUtils();
  const { begin, end } = useProgress();
  const { notifyError } = useNotify();
  const [loading, setLoading] = useState(false);

  return (
    <Disableable disabled={loading}>
      <FilledButton
        icon={<HeartFilled />}
        color={getColor("red", "darker")}
        className="py-1 m-1"
        onClick={async () => {
          trackEvent("action", "bookFavorite");

          setLoading(true);
          begin();

          try {
            await addToSpecialCollection(book.id, ObjectType.Book, SpecialCollection.Favorites);
          } catch (e) {
            notifyError(e);
          } finally {
            setLoading(false);
            end();
          }
        }}
      >
        <FormattedMessage id="pages.bookReader.buttons.favorite" />
      </FilledButton>
    </Disableable>
  );
};

const CollectionAddButton = ({ book }: { book: Book }) => {
  const [load, setLoad] = useState(false);

  return (
    <Dropdown onShow={() => setLoad(true)} overlay={load && <CollectionAddBookDropdownMenu book={book} />}>
      <FilledButton icon={<PlusOutlined />} color={getColor("gray", "darkest")} className="py-1 m-1">
        <FormattedMessage id="pages.bookReader.buttons.collectionAdd" />
      </FilledButton>
    </Dropdown>
  );
};

const DownloadButton = ({ book, content }: { book: Book; content: BookContent }) => {
  const { add } = useDownloads();
  const { isSupporter } = useClientInfo();

  let button = (
    <FilledButton
      icon={<CloudDownloadOutlined />}
      color={getColor("gray", "darkest")}
      className="py-1 m-1"
      onClick={() => {
        trackEvent("action", "bookDownload");

        add({
          type: "book",
          book: {
            id: book.id,
            contentId: content.id,
            primaryName: book.primaryName,
            englishName: book.englishName,
          },
        });
      }}
    >
      <FormattedMessage id="pages.bookReader.buttons.download" />
    </FilledButton>
  );

  if (!isSupporter && content.pageCount > NonSupporterPageLimit) {
    button = (
      <Tooltip
        placement="top"
        overlay={<FormattedMessage id="pages.bookReader.limits.download" values={{ count: NonSupporterPageLimit }} />}
      >
        <SupportLink>
          <Disableable disabled>{button}</Disableable>
        </SupportLink>
      </Tooltip>
    );
  }

  return button;
};
