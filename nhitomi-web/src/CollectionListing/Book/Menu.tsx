import React, { useState } from "react";
import { Tooltip } from "../../Components/Tooltip";
import { FormattedMessage } from "react-intl";
import { ObjectType } from "nhitomi-api";
import { PlusOutlined } from "@ant-design/icons";
import { RoundIconButton } from "../../Components/RoundIconButton";
import { useClient } from "../../ClientManager";
import { useProgress } from "../../ProgressManager";
import { useAlert, useNotify } from "../../NotificationManager";
import { Disableable } from "../../Components/Disableable";
import { useCollectionEditPrefetch } from "../Edit";
import { useLocalized } from "../../LocaleManager";
import { useDynamicPrefetch } from "../../Prefetch";

export const Menu = () => (
  <>
    <NewButton />
  </>
);

const NewButton = () => {
  const client = useClient();
  const { begin, end } = useProgress();
  const { notifyError } = useNotify();
  const { alert } = useAlert();
  const [loading, setLoading] = useState(false);
  const [prefetchNode, navigate] = useDynamicPrefetch(
    useCollectionEditPrefetch
  );

  const dummyName = useLocalized("components.collections.created.dummyName");

  return (
    <Tooltip
      placement="bottom"
      overlay={
        <FormattedMessage id="pages.collectionListing.book.menu.create" />
      }
    >
      <Disableable disabled={loading}>
        <RoundIconButton
          onClick={async () => {
            begin();
            setLoading(true);

            try {
              const collection = await client.collection.createCollection({
                createCollectionRequest: {
                  type: ObjectType.Book,
                  collection: {
                    name: dummyName,
                  },
                },
              });

              await navigate({ id: collection.id });

              alert(
                <FormattedMessage id="components.collections.created.success" />,
                "success"
              );
            } catch (e) {
              notifyError(e);
            } finally {
              end();
              setLoading(false);
            }
          }}
        >
          <PlusOutlined />
        </RoundIconButton>
      </Disableable>

      {prefetchNode}
    </Tooltip>
  );
};
