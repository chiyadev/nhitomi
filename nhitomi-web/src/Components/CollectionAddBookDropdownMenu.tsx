import React, { useState } from "react";
import { useClient, useClientInfo } from "../ClientManager";
import { useAlert, useNotify } from "../NotificationManager";
import { Collection, CollectionInsertPosition, ObjectType } from "nhitomi-api";
import { useAsync } from "../hooks";
import { Loading3QuartersOutlined, PlusOutlined } from "@ant-design/icons";
import { FormattedMessage } from "react-intl";
import { DropdownDivider, DropdownItem } from "./Dropdown";
import { Disableable } from "./Disableable";
import { useProgress } from "../ProgressManager";
import { CollectionContentLink } from "../CollectionContent";
import { usePageState } from "../state";
import { useLocalized } from "../LocaleManager";
import { useDynamicPrefetch } from "../Prefetch";
import { useCollectionEditPrefetch } from "../CollectionListing/Edit";
import { trackEvent } from "../track";
import { captureException } from "@sentry/react";

type BasicBook = {
  id: string;
};

export const CollectionAddBookDropdownMenu = ({ book }: { book: BasicBook }) => {
  const client = useClient();
  const { user } = useClientInfo();
  const { notifyError } = useNotify();
  const [collections, setCollections] = usePageState<Collection[]>("userCollections");

  useAsync(async () => {
    if (collections) return;

    try {
      if (!user) throw Error("Unauthenticated.");

      const { items } = await client.user.getUserCollections({
        id: user.id,
      });

      setCollections(items);
    } catch (e) {
      notifyError(e);
      captureException(e);

      setCollections(undefined);
    }
  }, []);

  return (
    <>
      {collections ? (
        <>
          {collections.map((collection) => (
            <Add key={`${book.id}/${collection.id}`} book={book} collection={collection} />
          ))}

          {collections.length ? <DropdownDivider /> : null}

          <Create book={book} />
        </>
      ) : (
        <Disableable disabled>
          <DropdownItem>
            <Loading3QuartersOutlined className="animate-spin" />
          </DropdownItem>
        </Disableable>
      )}
    </>
  );
};

const Add = ({ book, collection }: { book: BasicBook; collection: Collection }) => {
  const client = useClient();
  const { begin, end } = useProgress();
  const { alert } = useAlert();
  const { notifyError } = useNotify();
  const [loading, setLoading] = useState(false);

  return (
    <Disableable disabled={loading}>
      <DropdownItem
        onClick={async () => {
          trackEvent("action", "collectionAddBook");

          begin();
          setLoading(true);

          try {
            await client.collection.addCollectionItems({
              id: collection.id,
              addCollectionItemsRequest: {
                items: [book.id],
                position: CollectionInsertPosition.Start,
              },
            });

            alert(
              <FormattedMessage
                id="components.collections.added"
                values={{
                  name: (
                    <CollectionContentLink id={collection.id} className="text-blue">
                      {collection.name}
                    </CollectionContentLink>
                  ),
                }}
              />,
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
        {collection.name}
      </DropdownItem>
    </Disableable>
  );
};

const Create = ({ book }: { book: BasicBook }) => {
  const client = useClient();
  const { begin, end } = useProgress();
  const { notifyError } = useNotify();
  const { alert } = useAlert();
  const [loading, setLoading] = useState(false);
  const [prefetchNode, navigate] = useDynamicPrefetch(useCollectionEditPrefetch);

  const dummyName = useLocalized("components.collections.created.dummyName");

  return (
    <Disableable disabled={loading}>
      <DropdownItem
        icon={<PlusOutlined />}
        onClick={async () => {
          trackEvent("action", "collectionCreate");

          begin();
          setLoading(true);

          try {
            const created = await client.collection.createCollection({
              createCollectionRequest: {
                type: ObjectType.Book,
                collection: {
                  name: dummyName,
                },
                items: [book.id],
              },
            });

            await navigate({ id: created.id });

            alert(<FormattedMessage id="components.collections.created.success" />, "success");
          } catch (e) {
            notifyError(e);
          } finally {
            end();
            setLoading(false);
          }
        }}
      >
        <FormattedMessage id="components.collections.created.name" />
      </DropdownItem>

      {prefetchNode}
    </Disableable>
  );
};
