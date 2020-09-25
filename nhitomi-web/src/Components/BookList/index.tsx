import React, {
  ComponentType,
  ContextType,
  createContext,
  ReactNode,
  useCallback,
  useContext,
  useMemo,
  useRef,
} from "react";
import { BookApiGetBookImageRequest, BookContent, BookTags } from "nhitomi-api";
import { cx } from "emotion";
import { useSize } from "../../hooks";
import { Grid } from "./Grid";
import { ScraperTypes } from "../../orderedConstants";
import { TypedPrefetchLinkProps } from "../../Prefetch";
import { useConfig } from "../../ConfigManager";
import { Loader } from "./Loader";

export type BookListItem = {
  id: string;
  primaryName: string;
  englishName?: string;
  contents: BookContent[];
  tags?: BookTags;
};

const BookListContext = createContext<{
  items: BookListItem[];
  getCoverRequest?: (book: BookListItem, content: BookContent) => BookApiGetBookImageRequest;
  preferEnglishName?: boolean;
  overlayVisible?: boolean;

  LinkComponent?: ComponentType<{ id: string; contentId?: string } & TypedPrefetchLinkProps>;
  OverlayComponent?: ComponentType<{
    book: BookListItem;
    content?: BookContent;
    children: ReactNode;
  }>;
}>(undefined as any);

export function useBookList() {
  return useContext(BookListContext);
}

export const BookList = ({
  className,
  menu,
  empty,
  loadMore,
  ...context
}: ContextType<typeof BookListContext> & {
  className?: string;
  menu?: ReactNode;
  empty?: ReactNode;
  loadMore?: () => Promise<void>;
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const width = useSize(containerRef)?.width;

  const { items, getCoverRequest, preferEnglishName, overlayVisible, LinkComponent, OverlayComponent } = context;
  context = useMemo(
    () => ({
      items,
      getCoverRequest,
      preferEnglishName,
      overlayVisible,
      LinkComponent,
      OverlayComponent,
    }),
    [LinkComponent, OverlayComponent, getCoverRequest, items, overlayVisible, preferEnglishName]
  );

  return (
    <div ref={containerRef} className={cx("w-full relative", className)}>
      <BookListContext.Provider value={context}>
        {width && (
          <>
            <Grid width={width} menu={menu} empty={empty} />
            <Loader loadMore={loadMore} />
          </>
        )}
      </BookListContext.Provider>
    </div>
  );
};

export function useContentSelector(): (contents: BookContent[]) => BookContent | undefined {
  const [language] = useConfig("language");
  const [searchLanguages] = useConfig("searchLanguages");
  const languages = useMemo(() => [language, ...searchLanguages].filter((v, i, a) => a.indexOf(v) === i), [
    language,
    searchLanguages,
  ]);

  return useCallback(
    (contents) =>
      contents.sort((a, b) => {
        // respect language preference
        const language = indexCompare(languages, a.language, b.language);
        if (language) return language;

        // respect display scraper order
        const source = indexCompare(ScraperTypes, a.source, b.source);
        if (source) return source;

        // prefer newer contents
        return b.id.localeCompare(a.id);
      })[0],
    [languages]
  );
}

function indexCompare<T>(array: T[], a: T, b: T) {
  const x = array.indexOf(a);
  const y = array.indexOf(b);

  // prefer existing first
  if (x === -1) return 1;
  if (y === -1) return -1;

  return x - y;
}
