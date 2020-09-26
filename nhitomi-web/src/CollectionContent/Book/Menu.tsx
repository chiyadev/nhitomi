import React, { useState } from "react";
import { useClient, useClientInfo, useClientUtils } from "../../ClientManager";
import { useNotify } from "../../NotificationManager";
import { useProgress } from "../../ProgressManager";
import { Collection, SpecialCollection } from "nhitomi-api";
import { Dropdown, DropdownItem } from "../../Components/Dropdown";
import { useDynamicPrefetch, usePrefetch } from "../../Prefetch";
import { useCollectionListingPrefetch } from "../../CollectionListing";
import { RoundIconButton } from "../../Components/RoundIconButton";
import {
  CloudDownloadOutlined,
  DeleteOutlined,
  EditOutlined,
  EyeOutlined,
  HeartOutlined,
  InfoCircleOutlined,
  StarOutlined,
} from "@ant-design/icons";
import { FormattedMessage } from "react-intl";
import { FlatButton } from "../../Components/FlatButton";
import { Disableable } from "../../Components/Disableable";
import { CollectionEditLink } from "../../CollectionListing/Edit";
import { Tooltip } from "../../Components/Tooltip";
import { cx } from "emotion";
import { Anchor } from "../../Components/Anchor";
import { useBookReaderPrefetch } from "../../BookReader";
import { useContentSelector } from "../../Components/BookList";
import { RandomOutlined } from "../../Components/Icons/RandomOutlined";
import { DefaultQueryLimit } from "../../BookListing/search";
import { DownloadTargetAddArgs, useDownloads } from "../../DownloadManager";
import { SupportLink } from "../../Support";
import { trackEvent } from "../../umami";
import { captureException } from "@sentry/react";

export const Menu = ({ collection }: { collection: Collection }) => (
  <>
    <SpecialButton collection={collection} />
    <RandomButton collection={collection} />
    <DownloadButton collection={collection} />
    <EditButton collection={collection} />
    <DeleteButton collection={collection} />
    <HelpButton />
  </>
);

const SpecialButton = ({ collection }: { collection: Collection }) => {
  const { user } = useClientInfo();
  const { updateUser } = useClientUtils();
  const { begin, end } = useProgress();
  const { notifyError } = useNotify();
  const [loading, setLoading] = useState(false);

  if (!user) return null;

  const setSpecial = async (special: SpecialCollection) => {
    begin();
    setLoading(true);

    try {
      await updateUser((user) => ({
        ...user,
        specialCollections: {
          ...user.specialCollections,
          book: {
            ...user.specialCollections?.book,
            [special]: collection.id,
          },
        },
      }));
    } catch (e) {
      notifyError(e);
    } finally {
      end();
      setLoading(false);
    }
  };

  return (
    <Dropdown
      placement="bottom"
      overlay={
        <Disableable disabled={loading}>
          <DropdownItem
            icon={
              <HeartOutlined
                className={cx({
                  "text-red": user.specialCollections?.book?.favorites === collection.id,
                })}
              />
            }
            onClick={() => {
              trackEvent("action", "collectionSetFavorite");
              setSpecial(SpecialCollection.Favorites);
            }}
          >
            <FormattedMessage id="pages.collectionContent.book.menu.special.favorites" />
          </DropdownItem>

          <DropdownItem
            icon={
              <EyeOutlined
                className={cx({
                  "text-blue": user.specialCollections?.book?.later === collection.id,
                })}
              />
            }
            onClick={() => {
              trackEvent("action", "collectionSetLater");
              setSpecial(SpecialCollection.Later);
            }}
          >
            <FormattedMessage id="pages.collectionContent.book.menu.special.later" />
          </DropdownItem>
        </Disableable>
      }
    >
      <RoundIconButton>
        {user.specialCollections?.book?.favorites === collection.id ? (
          <HeartOutlined className="text-red" />
        ) : user.specialCollections?.book?.later === collection.id ? (
          <EyeOutlined className="text-blue" />
        ) : (
          <StarOutlined />
        )}
      </RoundIconButton>
    </Dropdown>
  );
};

const RandomButton = ({ collection }: { collection: Collection }) => {
  const client = useClient();
  const { notifyError } = useNotify();
  const [prefetchNode, navigate] = useDynamicPrefetch(useBookReaderPrefetch);
  const [loading, setLoading] = useState(false);

  if (!collection.items.length) return null;

  return (
    <Tooltip placement="bottom" overlay={<FormattedMessage id="pages.collectionContent.book.menu.random" />}>
      <Disableable disabled={loading}>
        <RoundIconButton
          onClick={async () => {
            trackEvent("action", "collectionRandom");

            setLoading(true);

            try {
              const book = await client.book.getBook({
                id: collection.items[Math.floor(Math.random() * collection.items.length)],
              });

              await navigate({ id: book.id });
            } catch (e) {
              notifyError(e);
              captureException(e);
            } finally {
              setLoading(false);
            }
          }}
        >
          <RandomOutlined />
        </RoundIconButton>
      </Disableable>

      {prefetchNode}
    </Tooltip>
  );
};

const DownloadButton = ({ collection }: { collection: Collection }) => {
  const { isSupporter } = useClientInfo();

  if (isSupporter) {
    return <DownloadButtonInner collection={collection} />;
  }

  return (
    <Tooltip placement="bottom" overlay={<FormattedMessage id="pages.collectionContent.book.menu.download" />}>
      <SupportLink>
        <RoundIconButton>
          <CloudDownloadOutlined />
        </RoundIconButton>
      </SupportLink>
    </Tooltip>
  );
};

const DownloadButtonInner = ({ collection }: { collection: Collection }) => {
  const client = useClient();
  const { begin, end } = useProgress();
  const { notifyError } = useNotify();
  const selectContent = useContentSelector();
  const { add } = useDownloads();
  const [loading, setLoading] = useState(false);

  if (!collection.items.length) return null;

  return (
    <Tooltip placement="bottom" overlay={<FormattedMessage id="pages.collectionContent.book.menu.download" />}>
      <Disableable disabled={loading}>
        <RoundIconButton
          onClick={async () => {
            trackEvent("action", "collectionDownload");

            setLoading(true);
            begin();

            try {
              // retrieve all books in chunks
              const books = (
                await Promise.all(
                  [...Array(Math.ceil(collection.items.length / DefaultQueryLimit))].map((_, i) =>
                    client.book.getBooks({
                      getBookManyRequest: {
                        ids: collection.items.slice(i * DefaultQueryLimit, (i + 1) * DefaultQueryLimit),
                      },
                    })
                  )
                )
              ).flat();

              const targets = books
                .map((book) => ({
                  book,
                  content: selectContent(book.contents),
                }))
                .filter((x) => x.content)
                .map(
                  ({ book, content }) =>
                    ({
                      type: "book",
                      book: {
                        id: book.id,
                        contentId: content?.id,
                        primaryName: book.primaryName,
                        englishName: book.englishName,
                      },
                    } as DownloadTargetAddArgs)
                );

              add(...targets);
            } catch (e) {
              notifyError(e);
              captureException(e);
            } finally {
              setLoading(false);
              end();
            }
          }}
        >
          <CloudDownloadOutlined />
        </RoundIconButton>
      </Disableable>
    </Tooltip>
  );
};

const EditButton = ({ collection }: { collection: Collection }) => (
  <Tooltip placement="bottom" overlay={<FormattedMessage id="pages.collectionContent.book.menu.edit" />}>
    <CollectionEditLink id={collection.id}>
      <RoundIconButton>
        <EditOutlined />
      </RoundIconButton>
    </CollectionEditLink>
  </Tooltip>
);

const DeleteButton = ({ collection }: { collection: Collection }) => {
  const client = useClient();
  const { begin, end } = useProgress();
  const { notifyError } = useNotify();
  const [loading, setLoading] = useState(false);
  const [, navigateListing] = usePrefetch(useCollectionListingPrefetch, {
    id: collection.ownerIds[0],
  });

  return (
    <Dropdown
      placement="bottom"
      padding={false}
      overlayClassName="flex flex-col space-y-2 p-2"
      overlay={
        <>
          <div>
            <FormattedMessage id="pages.collectionContent.book.menu.delete.warning" />
          </div>

          <Disableable disabled={loading}>
            <FlatButton
              icon={<DeleteOutlined />}
              className="w-full text-red"
              onClick={async () => {
                trackEvent("action", "collectionDelete");

                begin();
                setLoading(true);

                try {
                  await client.collection.deleteCollection({
                    id: collection.id,
                  });
                  await navigateListing();
                } catch (e) {
                  notifyError(e);
                } finally {
                  end();
                  setLoading(false);
                }
              }}
            >
              <FormattedMessage id="pages.collectionContent.book.menu.delete.button" />
            </FlatButton>
          </Disableable>
        </>
      }
    >
      <RoundIconButton>
        <DeleteOutlined />
      </RoundIconButton>
    </Dropdown>
  );
};

const HelpButton = () => (
  <Tooltip placement="bottom" overlay={<FormattedMessage id="pages.collectionContent.book.menu.help" />}>
    <Anchor target="_blank" href="https://github.com/chiyadev/nhitomi/wiki/Managing-collections-on-the-website">
      <RoundIconButton>
        <InfoCircleOutlined />
      </RoundIconButton>
    </Anchor>
  </Tooltip>
);
