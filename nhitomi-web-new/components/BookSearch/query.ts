import { ParsedUrlQuery } from "querystring";
import {
  BookQuery,
  BookQueryTags,
  BookSort,
  BookTag,
  LanguageType,
  QueryMatchMode,
  ScraperType,
  SortDirection,
} from "nhitomi-api";
import { BookTags } from "../../utils/constants";

export type QueryToken = {
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

export function tokenizeQuery(text: string): QueryToken[] {
  const results: QueryToken[] = [];

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

export function createQuery(options: ParsedUrlQuery) {
  const getOption = (key: string) => {
    const value = options[key];
    return (Array.isArray(value) ? value[0] : value) || undefined;
  };

  const query = tokenizeQuery(getOption("query") || "");
  const languages = (getOption("language")?.split(",") as LanguageType[]) || [];
  const sources = (getOption("source")?.split(",") as ScraperType[]) || [];
  const sort = (getOption("sort") as BookSort) || BookSort.UpdatedTime;
  const order = (getOption("order") as SortDirection) || SortDirection.Descending;

  const result: BookQuery = {
    mode: QueryMatchMode.All,
    language: !languages.length
      ? undefined
      : {
          values: languages,
          mode: QueryMatchMode.Any,
        },
    source: !sources.length
      ? undefined
      : {
          values: sources,
          mode: QueryMatchMode.Any,
        },
    sorting: [
      {
        value: sort,
        direction: order,
      },
    ],
    limit: 50,
  };

  const tags: BookQueryTags = (result.tags = {});

  function wrapTag(tag: string) {
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
  }

  for (const token of query) {
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
