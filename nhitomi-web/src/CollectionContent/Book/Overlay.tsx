import React, { ReactNode, useState } from "react";
import { BookListItem } from "../../Components/BookList";
import { Collection } from "nhitomi-api";
import { useClient } from "../../ClientManager";
import { useProgress } from "../../ProgressManager";
import { useAlert, useNotify } from "../../NotificationManager";
import { Disableable } from "../../Components/Disableable";
import { DropdownDivider, DropdownItem } from "../../Components/Dropdown";
import { DeleteOutlined } from "@ant-design/icons";
import { FormattedMessage } from "react-intl";
import { trackEvent } from "../../track";

export const Overlay = ({
  collection,
  book,
  children,
}: {
  collection: Collection;
  book: BookListItem;
  children?: ReactNode;
}) => {
  return (
    <>
      {children}

      <DropdownDivider />

      <DeleteItem collection={collection} book={book} />
    </>
  );
};

const DeleteItem = ({ collection, book }: { collection: Collection; book: BookListItem }) => {
  const client = useClient();
  const { begin, end } = useProgress();
  const { alert } = useAlert();
  const { notifyError } = useNotify();
  const [loading, setLoading] = useState(false);

  return (
    <Disableable disabled={loading}>
      <DropdownItem
        icon={<DeleteOutlined />}
        className="text-red"
        onClick={async () => {
          trackEvent("action", "collectionDeleteBook");

          begin();
          setLoading(true);

          try {
            await client.collection.removeCollectionItems({
              id: collection.id,
              collectionItemsRequest: {
                items: [book.id],
              },
            });

            alert(<FormattedMessage id="pages.collectionContent.book.overlay.delete.success" />, "success");
          } catch (e) {
            notifyError(e);
          } finally {
            end();
            setLoading(false);
          }
        }}
      >
        <FormattedMessage id="pages.collectionContent.book.overlay.delete.item" />
      </DropdownItem>
    </Disableable>
  );
};
