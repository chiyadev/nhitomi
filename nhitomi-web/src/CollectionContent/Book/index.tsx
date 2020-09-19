import React, { Dispatch, useMemo, useRef } from "react";
import { BookPrefetchResult } from "..";
import { Container } from "../../Components/Container";
import { useClient } from "../../ClientManager";
import { useNotify } from "../../NotificationManager";
import { useProgress } from "../../ProgressManager";
import { animated, useSpring } from "react-spring";
import { LoadContainer } from "../../Components/LoadContainer";
import { DefaultQueryLimit } from "../../BookListing/search";
import { Book } from "nhitomi-api";
import { BookList } from "../../Components/BookList";
import { useTabTitle } from "../../TitleSetter";
import { FormattedMessage } from "react-intl";
import { EmptyIndicator } from "../../Components/EmptyIndicator";
import { Menu } from "./Menu";
import { Overlay } from "./Overlay";

export const BookDisplay = ({
  result,
  setResult,
}: {
  result: BookPrefetchResult;
  setResult: Dispatch<BookPrefetchResult>;
}) => {
  const { collection, items } = result;

  useTabTitle(collection.name);

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

      <div className="py-4">
        <BookList
          items={items}
          menu={<Menu collection={collection} />}
          empty={
            <EmptyIndicator>
              <FormattedMessage id="pages.collectionContent.book.empty" />
            </EmptyIndicator>
          }
          OverlayComponent={(props) => <Overlay collection={collection} {...props} />}
        />

        <Loader result={result} setResult={setResult} />
      </div>
    </Container>
  );
};

const Loader = ({ result, setResult }: { result: BookPrefetchResult; setResult: Dispatch<BookPrefetchResult> }) => {
  const { collection } = result;

  const client = useClient();
  const { notifyError } = useNotify();
  const { begin, end: endProgress } = useProgress();

  const loadId = useRef(result.nextOffset >= collection.items.length ? -1 : 0);

  const style = useSpring({
    opacity: loadId.current < 0 ? 0 : 1,
  });

  return (
    <animated.div style={style}>
      <LoadContainer
        key={loadId.current} // recreate load container for each load
        className="w-full h-20"
        onLoad={async () => {
          if (loadId.current < 0) return;

          begin();

          try {
            const ids = collection.items.slice(result.nextOffset, result.nextOffset + DefaultQueryLimit);
            const moreResult = ids.length ? await client.book.getBooks({ getBookManyRequest: { ids } }) : [];

            if (loadId.current < 0) return;

            if (!moreResult.length) {
              loadId.current = -1;
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

            ++loadId.current;
          } catch (e) {
            notifyError(e);
          } finally {
            endProgress();
          }
        }}
      />
    </animated.div>
  );
};
