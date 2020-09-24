import React, { Dispatch, useCallback, useMemo, useRef } from "react";
import { BookPrefetchResult } from "..";
import { Container } from "../../Components/Container";
import { useClient } from "../../ClientManager";
import { useNotify } from "../../NotificationManager";
import { DefaultQueryLimit } from "../../BookListing/search";
import { Book } from "nhitomi-api";
import { BookList } from "../../Components/BookList";
import { useTabTitle } from "../../TitleSetter";
import { FormattedMessage } from "react-intl";
import { EmptyIndicator } from "../../Components/EmptyIndicator";
import { Menu } from "./Menu";
import { Overlay } from "./Overlay";
import { captureException } from "@sentry/react";

export const BookDisplay = ({
  result,
  setResult,
}: {
  result: BookPrefetchResult;
  setResult: Dispatch<BookPrefetchResult>;
}) => {
  const { collection } = result;

  useTabTitle(collection.name);

  const queryId = useRef(0);
  const client = useClient();
  const { notifyError } = useNotify();

  const loadMore = useCallback(async () => {
    const id = ++queryId.current;

    try {
      const ids = collection.items.slice(result.nextOffset, result.nextOffset + DefaultQueryLimit);
      const moreResult = ids.length ? await client.book.getBooks({ getBookManyRequest: { ids } }) : [];

      if (queryId.current === id) {
        // no more results
        if (!moreResult.length) {
          setResult({ ...result, nextOffset: result.collection.items.length });
          return;
        }

        // remove duplicates
        const items: Book[] = [];
        const exists: { [id: string]: true } = {};

        for (const item of [...result.items, ...moreResult]) {
          if (!exists[item.id]) items.push(item);
          exists[item.id] = true;
        }

        setResult({
          ...result,
          items,
          nextOffset: result.nextOffset + DefaultQueryLimit,
        });
      }
    } catch (e) {
      notifyError(e);
      captureException(e);

      setResult({ ...result, nextOffset: result.collection.items.length });
    }
  }, [result, setResult]);

  return (
    <Container className="divide-y divide-gray-darkest">
      {useMemo(
        () => (
          <div className="p-4">
            <div className="text-2xl">{collection.name}</div>
            <div className="text-sm text-gray-darker">{collection.description}</div>
          </div>
        ),
        [collection.description, collection.name]
      )}

      {useMemo(
        () => (
          <div className="py-4">
            <BookList
              items={result.items}
              menu={<Menu collection={collection} />}
              empty={
                <EmptyIndicator>
                  <FormattedMessage id="pages.collectionContent.book.empty" />
                </EmptyIndicator>
              }
              loadMore={result.nextOffset >= result.collection.items.length ? undefined : loadMore}
              OverlayComponent={(props) => <Overlay collection={collection} {...props} />}
            />
          </div>
        ),
        [result, collection, loadMore]
      )}
    </Container>
  );
};
