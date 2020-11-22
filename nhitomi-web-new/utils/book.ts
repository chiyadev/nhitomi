import { useCookieString } from "./config";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Book, BookContent, LanguageType } from "nhitomi-api";
import { ScraperTypes } from "./constants";
import { createApiClient } from "./client";

export function useContent(book: Book) {
  const selector = useContentSelector();
  return useMemo(() => selector(book.contents), [book.contents]);
}

export function useContentSelector(): (contents: BookContent[]) => BookContent {
  const [language] = useCookieString("language");
  const languages = (language?.split(",") as LanguageType[]) || [];

  return useCallback(
    (contents) =>
      contents.sort((a, b) => {
        function indexCompare<T>(array: T[], a: T, b: T) {
          const x = array.indexOf(a);
          const y = array.indexOf(b);

          // prefer existing first
          if (x === -1) return 1;
          if (y === -1) return -1;

          return x - y;
        }

        const language = indexCompare(languages, a.language, b.language);
        if (language) return language;

        const source = indexCompare(ScraperTypes, a.source, b.source);
        if (source) return source;

        return b.id.localeCompare(a.id);
      })[0],
    [languages]
  );
}

export function useBookImage(book: Book, content: BookContent, index: number): Blob | Error | undefined {
  const loadId = useRef(0);
  const [state, setState] = useState<Blob | Error>();

  useEffect(() => {
    (async () => {
      try {
        const id = ++loadId.current;
        const client = createApiClient();

        if (client) {
          const blob = await client.book.getBookImage({
            id: book.id,
            contentId: content.id,
            index,
          });

          if (id === loadId.current) {
            setState(blob);
          }
        } else {
          setState(Error("Unauthorized."));
        }
      } catch (e) {
        setState(e);
      }
    })();
  }, [book.id, content.id, index]);

  return state;
}
