import React, { useState } from "react";
import { BookListItem } from "../../Components/BookList";
import { DeleteOutlined, EditOutlined, ExpandAltOutlined } from "@ant-design/icons";
import { FormattedMessage } from "react-intl";
import { DropdownItem, DropdownSubMenu } from "../../Components/Dropdown";
import { usePrefetch } from "../../Prefetch";
import { useCollectionListingPrefetch } from "..";
import { User } from "nhitomi-api";
import { useProgress } from "../../ProgressManager";
import { Disableable } from "../../Components/Disableable";
import { useNotify } from "../../NotificationManager";
import { useClient } from "../../ClientManager";
import { CollectionContentLink } from "../../CollectionContent";
import { CollectionEditLink } from "../Edit";
import { trackEvent } from "../../track";

export const Overlay = ({ user, book: { id } }: { user: User; book: BookListItem }) => {
  return (
    <>
      <OpenInNewTabItem id={id} />
      <EditItem id={id} />
      <DeleteItem user={user} id={id} />
    </>
  );
};

const OpenInNewTabItem = ({ id }: { id: string }) => (
  <CollectionContentLink id={id} target="_blank" rel="noopener noreferrer">
    <DropdownItem icon={<ExpandAltOutlined />}>
      <FormattedMessage id="pages.collectionListing.book.overlay.openNewTab" />
    </DropdownItem>
  </CollectionContentLink>
);

const EditItem = ({ id }: { id: string }) => (
  <CollectionEditLink id={id}>
    <DropdownItem icon={<EditOutlined />}>
      <FormattedMessage id="pages.collectionListing.book.overlay.edit" />
    </DropdownItem>
  </CollectionEditLink>
);

const DeleteItem = ({ user, id }: { user: User; id: string }) => {
  const client = useClient();
  const { begin, end } = useProgress();
  const { notifyError } = useNotify();
  const [loading, setLoading] = useState(false);

  const [, navigate] = usePrefetch(useCollectionListingPrefetch, {
    id: user.id,
  });

  return (
    <DropdownSubMenu
      icon={<DeleteOutlined />}
      name={<FormattedMessage id="pages.collectionListing.book.overlay.delete.item" />}
    >
      <Disableable disabled={loading}>
        <DropdownItem
          icon={<DeleteOutlined />}
          className="text-red"
          onClick={async () => {
            trackEvent("action", "collectionDelete");

            begin();
            setLoading(true);

            try {
              await client.collection.deleteCollection({ id });
              await navigate("replace");
            } catch (e) {
              notifyError(e);
            } finally {
              end();
              setLoading(false);
            }
          }}
        >
          <FormattedMessage id="pages.collectionListing.book.overlay.delete.confirm" />
        </DropdownItem>
      </Disableable>
    </DropdownSubMenu>
  );
};
