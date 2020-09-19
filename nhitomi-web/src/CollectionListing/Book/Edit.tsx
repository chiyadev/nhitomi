import React, { useState } from "react";
import { FormattedMessage } from "react-intl";
import { Collection, CollectionInsertPosition } from "nhitomi-api";
import { FlatButton } from "../../Components/FlatButton";
import { CopyOutlined } from "@ant-design/icons";
import { useClient } from "../../ClientManager";
import { useProgress } from "../../ProgressManager";
import { useAlert, useNotify } from "../../NotificationManager";
import { useDynamicPrefetch } from "../../Prefetch";
import { useCollectionContentPrefetch } from "../../CollectionContent";
import { Disableable } from "../../Components/Disableable";

export const Edit = ({ collection }: { collection: Collection }) => (
  <>
    <Duplicate collection={collection} />
  </>
);

const Duplicate = ({ collection }: { collection: Collection }) => {
  const client = useClient();
  const { begin, end } = useProgress();
  const { notifyError } = useNotify();
  const { alert } = useAlert();
  const [loading, setLoading] = useState(false);
  const [prefetchNode, navigate] = useDynamicPrefetch(useCollectionContentPrefetch);

  return (
    <div className="p-4 space-y-4">
      <div>
        <div>
          <FormattedMessage id="pages.collectionListing.edit.book.duplicate.name" />
        </div>
        <div className="text-sm text-gray-darker">
          <FormattedMessage id="pages.collectionListing.edit.book.duplicate.description" />
        </div>
      </div>

      <Disableable disabled={loading}>
        <FlatButton
          icon={<CopyOutlined />}
          onClick={async () => {
            begin();
            setLoading(true);

            try {
              let created = await client.collection.createCollection({
                createCollectionRequest: {
                  type: collection.type,
                  collection,
                },
              });

              if (collection.items.length) {
                created = await client.collection.addCollectionItems({
                  id: created.id,
                  addCollectionItemsRequest: {
                    items: collection.items,
                    position: CollectionInsertPosition.Start,
                  },
                });
              }

              await navigate({ id: created.id });

              alert(<FormattedMessage id="pages.collectionListing.edit.book.duplicate.success" />, "success");
            } catch (e) {
              notifyError(e);
            } finally {
              end();
              setLoading(false);
            }
          }}
        >
          <FormattedMessage id="pages.collectionListing.edit.book.duplicate.name" />
        </FlatButton>
      </Disableable>

      {prefetchNode}
    </div>
  );
};
