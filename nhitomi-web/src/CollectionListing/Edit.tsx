import React, { useCallback, useMemo, useState } from "react";
import { Collection, ObjectType, User } from "nhitomi-api";
import {
  BackLink,
  PrefetchGenerator,
  PrefetchLink,
  TypedPrefetchLinkProps,
  usePostfetch,
  usePrefetch,
} from "../Prefetch";
import { useClient } from "../ClientManager";
import { PageContainer } from "../Components/PageContainer";
import { usePageState } from "../state";
import { Container } from "../Components/Container";
import { FormattedMessage } from "react-intl";
import { Input } from "../Components/Input";
import { FilledButton } from "../Components/FilledButton";
import { CheckOutlined, DeleteOutlined, LeftOutlined, Loading3QuartersOutlined } from "@ant-design/icons";
import { FlatButton } from "../Components/FlatButton";
import { Disableable } from "../Components/Disableable";
import { useNotify } from "../NotificationManager";
import { useTabTitle } from "../TitleSetter";
import { useLocalized } from "../LocaleManager";
import { getColor } from "../theme";
import { useCollectionContentPrefetch } from "../CollectionContent";
import { useCollectionListingPrefetch } from ".";
import { Edit as BookEdit } from "./Book/Edit";
import { trackEvent } from "../track";

export type PrefetchResult = { collection: Collection; owner: User };
export type PrefetchOptions = { id: string };

export const useCollectionEditPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ id }) => {
  const client = useClient();

  return {
    destination: {
      path: `/collections/${id}/edit`,
    },

    fetch: async () => {
      const collection = await client.collection.getCollection({ id });
      const owner = await client.user.getUser({ id: collection.ownerIds[0] });

      return { collection, owner };
    },
  };
};

export const CollectionEditLink = ({ id, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useCollectionEditPrefetch} options={{ id }} {...props} />
);

export const CollectionEdit = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useCollectionEditPrefetch, {
    requireAuth: true,
    ...options,
  });

  if (!result) return null;

  return (
    <PageContainer>
      <Loaded {...result} />
    </PageContainer>
  );
};

const Loaded = ({ collection, owner }: PrefetchResult) => {
  useTabTitle(collection.name, useLocalized("pages.collectionListing.edit.title"));

  const [loading, setLoading] = useState(false);

  const client = useClient();
  const { notifyError } = useNotify();
  const [, navigateListing] = usePrefetch(useCollectionListingPrefetch, {
    id: owner.id,
  });
  const [, navigateCollection] = usePrefetch(useCollectionContentPrefetch, {
    id: collection.id,
  });

  const [name, setName] = usePageState("name", collection.name);
  const [description, setDescription] = usePageState("description", collection.description);

  const submit = useCallback(async () => {
    if (loading) return;

    trackEvent("action", "collectionUpdate");
    setLoading(true);

    try {
      await client.collection.updateCollection({
        id: collection.id,
        collectionBase: {
          name,
          description,
        },
      });

      await navigateCollection();
    } catch (e) {
      notifyError(e);
    } finally {
      setLoading(false);
    }
  }, [client.collection, collection.id, description, loading, name, navigateCollection, notifyError]);

  const delette = useCallback(async () => {
    if (loading) return;

    trackEvent("action", "collectionDelete");
    setLoading(true);

    try {
      await client.collection.deleteCollection({ id: collection.id });
      await navigateListing();
    } catch (e) {
      notifyError(e);
    } finally {
      setLoading(false);
    }
  }, [client.collection, collection.id, loading, navigateListing, notifyError]);

  return (
    <Container className="divide-y divide-gray-darkest">
      {useMemo(
        () => (
          <div className="p-4">
            <div className="text-2xl">
              <FormattedMessage id="pages.collectionListing.edit.title" />
            </div>
            <div className="text-sm text-gray-darker">
              <FormattedMessage
                id="pages.collectionListing.edit.subtitle"
                values={{ collection: collection.name, owner: owner.username }}
              />
            </div>
          </div>
        ),
        [collection.name, owner.username]
      )}

      <Disableable disabled={loading}>
        <div className="space-y-8 divide-y divide-gray-darkest">
          <div className="p-4 space-y-4">
            {useMemo(
              () => (
                <div>
                  <div className="mb-1">
                    <FormattedMessage id="pages.collectionListing.edit.name" />
                  </div>

                  <Input
                    className="w-full max-w-sm text-sm"
                    autoFocus
                    allowClear
                    value={name}
                    setValue={setName}
                    onSubmit={submit}
                  />
                </div>
              ),
              [name, setName, submit]
            )}

            {useMemo(
              () => (
                <div>
                  <div className="mb-1">
                    <FormattedMessage id="pages.collectionListing.edit.description" />
                  </div>

                  <Input
                    type="textarea"
                    className="w-full max-w-sm text-sm"
                    value={description}
                    setValue={setDescription}
                    onSubmit={submit}
                  />
                </div>
              ),
              [description, setDescription, submit]
            )}

            {useMemo(
              () => (
                <div className="space-x-1">
                  <BackLink>
                    <FlatButton icon={<LeftOutlined />}>
                      <FormattedMessage id="pages.collectionListing.edit.cancel" />
                    </FlatButton>
                  </BackLink>

                  <FlatButton color={getColor("red", "darker")} onClick={delette} icon={<DeleteOutlined />}>
                    <FormattedMessage id="pages.collectionListing.edit.delete" />
                  </FlatButton>

                  <FilledButton
                    color={getColor("blue")}
                    onClick={submit}
                    icon={loading ? <Loading3QuartersOutlined className="animate-spin" /> : <CheckOutlined />}
                  >
                    <FormattedMessage id="pages.collectionListing.edit.submit" />
                  </FilledButton>
                </div>
              ),
              [delette, loading, submit]
            )}
          </div>

          {useMemo(() => collection.type === ObjectType.Book && <BookEdit collection={collection} />, [collection])}
        </div>
      </Disableable>
    </Container>
  );
};
