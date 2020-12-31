import Router, { useRouter } from "next/router";
import { SetStateAction, useCallback, useMemo } from "react";
import { BookSort, SortDirection } from "nhitomi-api";
import { ParsedUrlQuery } from "querystring";
import { ScraperTypes } from "./constants";

type Dispatch<T = any> = (value: T, mode?: "replace" | "push") => Promise<boolean>;

export function useQueryString(
  key: string
): [string | string[] | undefined, Dispatch<SetStateAction<string | string[] | undefined>>] {
  return [
    useRouter().query[key],
    useCallback(
      (value, mode = "replace") => {
        if (typeof value === "function") {
          value = value(Router.query[key]);
        }

        return (mode === "replace" ? Router.replace : Router.push).call(
          Router,
          {
            query: {
              ...Router.query,
              [key]: typeof value === "undefined" ? [] : value,
            },
          },
          undefined,
          {
            shallow: mode === "replace",
          }
        );
      },
      [key]
    ),
  ];
}

export type Queries = {
  id: string;
  contentId: string;
  query: string;
  sort: BookSort;
  order: SortDirection;
  source: string;
};

export const DefaultQueries: Queries = {
  id: "",
  contentId: "",
  query: "",
  sort: BookSort.UpdatedTime,
  order: SortDirection.Descending,
  source: ScraperTypes.join(","),
};

function parseValueOrDefault(key: keyof Queries, value?: string | string[]): any {
  if (Array.isArray(value) && value.length) {
    return value[0];
  } else {
    return value || DefaultQueries[key];
  }
}

export function parseQueries(queries: ParsedUrlQuery): Queries {
  const result = { ...DefaultQueries };

  for (const key in queries) {
    (result as any)[key] = parseValueOrDefault(key as any, queries[key]);
  }

  return result;
}

export function useQuery<TKey extends keyof Queries>(
  key: TKey
): [Queries[TKey], Dispatch<SetStateAction<Queries[TKey]>>] {
  const [value, setValue] = useQueryString(key);

  return [
    useMemo(() => parseValueOrDefault(key, value), [key, value]),
    useCallback(
      (newValue, mode) => {
        return setValue((value) => {
          if (typeof newValue === "function") {
            return newValue(parseValueOrDefault(key, value));
          } else {
            return newValue;
          }
        }, mode);
      },
      [key]
    ),
  ];
}
