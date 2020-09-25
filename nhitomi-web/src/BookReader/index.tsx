import React, { useMemo, useRef } from "react";
import { PrefetchGenerator, PrefetchLink, TypedPrefetchLinkProps, usePostfetch } from "../Prefetch";
import { Book, BookContent } from "nhitomi-api";
import { useClient } from "../ClientManager";
import { useScrollShortcut } from "../shortcut";
import { PageContainer } from "../Components/PageContainer";
import { Container } from "../Components/Container";
import { useSize } from "../hooks";
import { Info } from "./Info";
import { Background } from "./Background";
import { Reader } from "./Reader";
import { LayoutSetter } from "./LayoutSetter";
import { CursorVisibility } from "./CursorVisibility";
import { useTabTitle } from "../TitleSetter";
import { useConfig } from "../ConfigManager";
import { SupportBanner } from "./SupportBanner";
import { useContentSelector } from "../Components/BookList";

export type PrefetchResult = { book: Book; content: BookContent };
export type PrefetchOptions = { id: string; contentId?: string };

export const useBookReaderPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ id, contentId }) => {
  const client = useClient();
  const selectContent = useContentSelector();

  return {
    destination: {
      path: contentId ? `/books/${id}/contents/${contentId}` : `/books/${id}`,
    },

    fetch: async () => {
      const book = await client.book.getBook({ id });
      const content = contentId ? book.contents.find((c) => c.id === contentId) : selectContent(book.contents);

      if (!content) {
        throw Error(`'${contentId}' not found.`);
      }

      return { book, content };
    },
  };
};

export const BookReaderLink = ({ id, contentId, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useBookReaderPrefetch} options={{ id, contentId }} {...props} />
);

export const BookReader = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useBookReaderPrefetch, {
    requireAuth: true,
    ...options,
  });

  useScrollShortcut();

  return useMemo(() => {
    if (!result) return null;

    return (
      <PageContainer key={`${result.book.id}/${result.content.id}`}>
        <Loaded book={result.book} content={result.content} />
      </PageContainer>
    );
  }, [result]);
};

const Loaded = ({ book, content }: PrefetchResult) => {
  const [preferEnglishName] = useConfig("bookReaderPreferEnglishName");

  useTabTitle((preferEnglishName && book.englishName) || book.primaryName);

  const infoRef = useRef(null);
  const { width: infoWidth, height: infoHeight } = useSize(infoRef) || { width: 0, height: 0 };

  return (
    <>
      <LayoutSetter />

      {infoHeight && <Background book={book} content={content} scrollHeight={infoHeight} />}

      <div className="space-y-8">
        <div ref={infoRef}>
          <Container>
            <Info book={book} content={content} />
            <SupportBanner book={book} content={content} />
          </Container>
        </div>

        {infoWidth && (
          <CursorVisibility>
            <Reader book={book} content={content} viewportWidth={infoWidth} />
          </CursorVisibility>
        )}
      </div>
    </>
  );
};
