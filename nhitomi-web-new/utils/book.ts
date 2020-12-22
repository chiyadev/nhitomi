import { useCallback, useMemo } from "react";
import {
  Book,
  BookContent,
  BookQuery,
  BookQueryTags,
  BookSort,
  BookTag,
  LanguageType,
  QueryMatchMode,
  ScraperType,
  SortDirection,
} from "nhitomi-api";
import { BookTags, LanguageTypes, ScraperTypes } from "./constants";
import { useConfig } from "./config";
import { useQuery } from "./query";

export function useBookContent(book: Book) {
  const selector = useBookContentSelector();
  return useMemo(() => selector(book.contents), [book.contents]);
}

export function useBookContentSelector(): (contents: BookContent[]) => BookContent {
  const [languages] = useConfig("searchLanguages");
  const [source] = useQuery("source");

  return useCallback((contents) => selectBookContent(contents, languages, source.split(",") as ScraperType[]), [
    languages,
    source,
  ]);
}

function reorderByRef<T>(array: T[], reference: T[]) {
  const set = new Set(array);
  return reference.filter((v) => set.has(v));
}

export function selectBookContent(
  contents: BookContent[],
  languages: LanguageType[],
  sources: ScraperType[]
): BookContent {
  languages = reorderByRef(languages, LanguageTypes);
  sources = reorderByRef(sources, ScraperTypes);

  return contents.sort((a, b) => {
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

    const source = indexCompare(sources, a.source, b.source);
    if (source) return source;

    return b.id.localeCompare(a.id);
  })[0];
}

export type BookQueryToken = {
  index: number;
  begin: number;
  end: number;
  text: string;
  display: string;
} & (
  | {
      type: "tag";
      tag: BookTag;
      value: string;
    }
  | {
      type: "url";
    }
  | {
      type: "other";
    }
);

const tagRegex = /(?<tag>\w+):(?<value>\S+)/gis;

export function tokenizeBookQuery(text: string): BookQueryToken[] {
  const results: BookQueryToken[] = [];

  let match: RegExpExecArray | null;
  let start = 0;

  const addOther = (start: number, end: number) => {
    const s = text.substring(start, end);
    let url = false;

    try {
      url = !!new URL(s);
    } catch {
      /* ignored */
    }

    results.push({
      type: url ? "url" : "other",
      index: start,
      begin: start + (s.length - s.trimStart().length),
      end: start + s.trimEnd().length,
      text: s,
      display: s.replace(/_/g, " ").trim(),
    });
  };

  const addTag = (start: number, end: number, tag: BookTag, value: string) => {
    results.push({
      type: "tag",
      index: start,
      begin: start,
      end,
      text: text.substring(start, end),
      tag,
      value,
      display: value.replace(/_/g, " ").trim(),
    });
  };

  while ((match = tagRegex.exec(text))) {
    const tag = (match.groups?.tag || "") as BookTag;
    const value = match.groups?.value || "";

    if (BookTags.findIndex((t) => t.toLowerCase() === tag.toLowerCase()) === -1) continue;

    if (start < match.index) {
      addOther(start, match.index);
    }

    addTag(match.index, tagRegex.lastIndex, tag, value);
    start = tagRegex.lastIndex;
  }

  if (start < text.length) {
    addOther(start, text.length);
  }

  return results;
}

export function createBookQuery(
  query: string,
  languages: LanguageType[],
  sources: ScraperType[],
  sort: BookSort,
  order: SortDirection
) {
  const result: BookQuery = {
    mode: QueryMatchMode.All,
    language: !languages?.length
      ? undefined
      : {
          values: languages,
          mode: QueryMatchMode.Any,
        },
    source: !sources?.length
      ? undefined
      : {
          values: sources,
          mode: QueryMatchMode.Any,
        },
    sorting: [
      {
        value: sort || BookSort.UpdatedTime,
        direction: order || SortDirection.Descending,
      },
    ],
    limit: 50,
  };

  const tokens = tokenizeBookQuery(query || "");
  const tags: BookQueryTags = (result.tags = {});

  const wrapTag = (tag: string) => {
    let negate = false;

    if (tag.startsWith("-")) {
      negate = true;
      tag = tag.substring(1);
    }

    // wrap in quotes for phrase match for tags with spaces
    tag = `"${tag}"`;

    if (negate) {
      tag = "-" + tag;
    }

    return tag;
  };

  for (const token of tokens) {
    switch (token.type) {
      case "tag":
        const value = token.display.substring(token.display.indexOf(":"));

        if (value) {
          (tags[token.tag] || (tags[token.tag] = { values: [], mode: QueryMatchMode.All })).values.push(wrapTag(value));
        }

        break;

      default:
        if (token.display) {
          (result.all || (result.all = { values: [], mode: QueryMatchMode.All })).values.push(token.display);
        }

        break;
    }
  }

  return result;
}
