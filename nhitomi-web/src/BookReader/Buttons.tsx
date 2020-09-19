import React, { useState } from "react";
import { useClientUtils } from "../ClientManager";
import { useNotify } from "../NotificationManager";
import { Book, ObjectType, SpecialCollection } from "nhitomi-api";
import { PrefetchResult } from ".";
import { FilledButton } from "../Components/FilledButton";
import { HeartFilled, PlusOutlined } from "@ant-design/icons";
import { getColor } from "../theme";
import { FormattedMessage } from "react-intl";
import { Dropdown } from "../Components/Dropdown";
import { CollectionAddBookDropdownMenu } from "../Components/CollectionAddBookDropdownMenu";
import { useProgress } from "../ProgressManager";
import { Disableable } from "../Components/Disableable";

export const Buttons = ({ book }: PrefetchResult) => {
  return (
    <div className="flex flex-row space-x-2">
      <FavoriteButton book={book} />
      <CollectionAddButton book={book} />
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
        className="py-1"
        onClick={async () => {
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
      <FilledButton icon={<PlusOutlined />} color={getColor("gray", "darkest")} className="py-1">
        <FormattedMessage id="pages.bookReader.buttons.collectionAdd" />
      </FilledButton>
    </Dropdown>
  );
};
