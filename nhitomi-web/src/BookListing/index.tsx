import React, { Dispatch, useCallback, useMemo, useRef } from "react";
import { convertQuery, DefaultQueryLimit, performQuery, SearchQuery } from "./search";
import { usePageState, useQueryState } from "../state";
import { PrefetchGenerator, PrefetchLink, TypedPrefetchLinkProps, usePostfetch } from "../Prefetch";
import { Book, BookSearchResult, BookSort, SortDirection } from "nhitomi-api";
import { SearchInput } from "./SearchInput";
import { BookList } from "../Components/BookList";
import { useNotify } from "../NotificationManager";
import { useClient, useClientInfo } from "../ClientManager";
import { useProgress } from "../ProgressManager";
import { useScrollShortcut } from "../shortcut";
import { useConfig } from "../ConfigManager";
import { animated, useSpring } from "react-spring";
import { PageContainer } from "../Components/PageContainer";
import stringify from "json-stable-stringify";
import { Container } from "../Components/Container";
import { Menu } from "./Menu";
import { useTabTitle } from "../TitleSetter";
import { useLocalized } from "../LocaleManager";
import { EmptyIndicator } from "../Components/EmptyIndicator";
import { FormattedMessage } from "react-intl";
import { useAsync } from "../hooks";
import { captureException } from "@sentry/react";

export type PrefetchResult = BookSearchResult & { nextOffset: number };
export type PrefetchOptions = { query?: SearchQuery };

export const useBookListingPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({
  mode,
  query: targetQuery,
}) => {
  const client = useClient();
  const { info } = useClientInfo();
  const [languages] = useConfig("searchLanguages");
  const [currentQuery] = useQueryState<SearchQuery>();

  const query = targetQuery || (mode === "postfetch" && currentQuery) || {};

  query.query = query.query || "";
  query.sources = query.sources?.length ? query.sources : undefined;

  // sort by updated time
  query.sort = query.sort || BookSort.UpdatedTime;
  query.order = query.order || SortDirection.Descending;

  // use configured languages if unspecified
  query.langs = query.langs?.length ? query.langs : languages;

  return {
    destination: {
      path: "/books",
      query,
      state: (s) => ({
        ...s,
        query: { value: query, version: Math.random() }, // synchronize effective query immediately
      }),
    },

    fetch: async () => {
      const result = await performQuery(client, info, query);

      return { ...result, nextOffset: DefaultQueryLimit };
    },
  };
};

export const BookListingLink = ({ query, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useBookListingPrefetch} options={{ query }} {...props} />
);

export const BookListing = (options: PrefetchOptions) => {
  const { result, setResult } = usePostfetch(useBookListingPrefetch, {
    requireAuth: true,
    ...options,
  });

  useScrollShortcut();

  return useMemo(() => {
    if (!result) return null;

    return (
      <PageContainer>
        <Loaded result={result} setResult={setResult} />
      </PageContainer>
    );
  }, [result]);
};

const Loaded = ({ result, setResult }: { result: PrefetchResult; setResult: Dispatch<PrefetchResult> }) => {
  const [query] = useQueryState<SearchQuery>();
  const queryId = useRef(0);

  useTabTitle(query.query, useLocalized("pages.bookListing.title"));

  const client = useClient();
  const { info } = useClientInfo();
  const { notifyError } = useNotify();
  const { begin, end } = useProgress();

  // displayed results may not represent the current query if we navigated before storing the results
  // another way to put it: user searches, page navigates but search is still happening so the current query and displayed results don't match
  const [effectiveQuery, setEffectiveQuery] = usePageState<SearchQuery>("query", query);

  // serialized query string is used for comparison
  const queryCmp = useMemo(() => stringify(query), [query]);
  const effectiveQueryCmp = useMemo(() => stringify(effectiveQuery || {}), [effectiveQuery]);

  // perform search when query changes
  useAsync(async () => {
    if (queryCmp === effectiveQueryCmp) return;

    begin();

    const id = ++queryId.current;

    try {
      const result = await performQuery(client, info, query);

      if (queryId.current === id) {
        setResult({ ...result, nextOffset: DefaultQueryLimit });
        setEffectiveQuery(query);
      }
    } catch (e) {
      notifyError(e);
      captureException(e);
    } finally {
      end();
    }
  }, [queryCmp, effectiveQueryCmp]);

  const loadMore = useCallback(async () => {
    const id = ++queryId.current;

    try {
      const moreResult = await client.book.searchBooks({
        bookQuery: {
          ...convertQuery(query),
          offset: result.nextOffset,
        },
      });

      if (queryId.current === id) {
        // no more results
        if (!moreResult.items.length) {
          setResult({ ...result, nextOffset: result.total });
          return;
        }

        // remove duplicates
        const items: Book[] = [];
        const exists: { [id: string]: true } = {};

        for (const item of [...result.items, ...moreResult.items]) {
          if (!exists[item.id]) items.push(item);
          exists[item.id] = true;
        }

        setResult({
          ...result,
          ...moreResult,
          items,
          nextOffset: result.nextOffset + DefaultQueryLimit,
        });
      }
    } catch (e) {
      notifyError(e);
      captureException(e);

      setResult({ ...result, nextOffset: result.total });
    }
  }, [client, result, setResult]);

  return (
    <Container>
      {useMemo(
        () => (
          <Input result={result} />
        ),
        [result]
      )}

      {useMemo(
        () => (
          <BookList
            items={result.items}
            menu={<Menu />}
            empty={
              <EmptyIndicator>
                <FormattedMessage id="pages.bookListing.empty" />
              </EmptyIndicator>
            }
            loadMore={result.nextOffset >= result.total ? undefined : loadMore}
          />
        ),
        [result, setResult, loadMore]
      )}
    </Container>
  );
};

const Input = ({ result }: { result: PrefetchResult }) => {
  const style = useSpring({
    from: { opacity: 0, marginTop: -5, paddingBottom: 5 },
    to: { opacity: 1, marginTop: 0, paddingBottom: 0 },
  });

  return (
    <div className="mx-auto p-4 w-full max-w-2xl sticky top-0 z-20">
      <animated.div style={style} className="w-full">
        <SearchInput result={result} className="shadow-lg w-full" />
      </animated.div>
    </div>
  );
};
