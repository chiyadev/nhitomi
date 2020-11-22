import React, { memo, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Flex } from "@chakra-ui/react";
import Input from "./Input";
import Suggest from "./Suggest";
import { createApiClient } from "../../../utils/client";
import { BookTag, SuggestItem } from "nhitomi-api";
import levenshtein from "js-levenshtein";
import { tokenizeQuery } from "../query";

export type TagSuggestion = SuggestItem & { type: "tag"; tag: BookTag };
export type BookSuggestion = SuggestItem & { type: "book" };

const Content = ({ value, setValue }: { value: string; setValue: (value: string) => Promise<void> }) => {
  const [display, setDisplay] = useState(value);
  const tokens = useMemo(() => tokenizeQuery(display), [display]);

  const [load, setLoad] = useState(0);
  const setLoading = useCallback((value: boolean) => setLoad((i) => (value ? i + 1 : i - 1)), []);

  const inputRef = useRef<HTMLInputElement>(null);
  const [inputIndex, setInputIndex] = useState(0);
  const [inputFocus, setInputFocus] = useState(false);

  useEffect(() => {
    const input = inputRef.current;
    if (!input) return;

    const handler = () => {
      setInputIndex(input.selectionEnd || input.selectionStart || 0);
    };

    handler();

    // unfortunately input doesn't have a caret event
    input.addEventListener("change", handler);
    input.addEventListener("mousedown", handler);
    input.addEventListener("mouseup", handler);
    input.addEventListener("keydown", handler);
    input.addEventListener("keyup", handler);

    return () => {
      input.removeEventListener("change", handler);
      input.removeEventListener("mousedown", handler);
      input.removeEventListener("mouseup", handler);
      input.removeEventListener("keydown", handler);
      input.removeEventListener("keyup", handler);
    };
  }, [inputRef]);

  const suggestTimeout = useRef<number>();
  const [suggest, setSuggest] = useState<TagSuggestion>();
  const [suggestText, setSuggestText] = useState("");
  const [suggestTags, setSuggestTags] = useState<TagSuggestion[]>([]);
  const [suggestBooks, setSuggestBooks] = useState<BookSuggestion[]>([]);

  const suggestQueryToken = useMemo(() => {
    for (const token of [...tokens].reverse()) {
      if (token.begin <= inputIndex) {
        return token;
      }
    }
  }, [tokens, inputIndex]);

  const suggestQuery = useMemo(() => {
    switch (suggestQueryToken?.type) {
      case "tag":
        return suggestQueryToken.value.replace(/_/g, " ");

      default:
        return suggestQueryToken?.text.trim() || "";
    }
  }, [suggestQueryToken]);

  const complete = useCallback(
    async (item?: TagSuggestion) => {
      const token = suggestQueryToken;
      item = item || suggest;

      if (!token || !item) {
        if (display.trim()) {
          setLoading(true);

          try {
            await setValue(display);
          } finally {
            setLoading(false);
          }
        }

        return;
      }

      let text = display;

      const replacement = `${item.tag}:${item.text.replace(/\s/g, "_")}`;

      const remove = (s: string, start: number, end: number) => s.substring(0, start) + s.substring(end);
      const insert = (s: string, index: number, value: string) => s.substring(0, index) + value + s.substring(index);

      text = remove(text, token.begin, token.end);
      text = insert(text, token.begin, replacement);

      let caret = token.begin + replacement.length;

      if (!text[caret + 1]) {
        text = insert(text, caret, " ");
        caret += 1;
      }

      setDisplay(text);
      setSuggest(item);

      setTimeout(() => {
        const input = inputRef.current;

        if (input) {
          setInputIndex((input.selectionStart = input.selectionEnd = caret));
          input.focus();
        }
      });
    },
    [suggest, suggestQueryToken, display, setLoading, inputRef]
  );

  const moveSuggest = useCallback(
    (delta) => {
      setSuggest(
        !suggestTags.length
          ? undefined
          : suggestTags[
              (suggestTags.length + (suggest ? suggestTags.indexOf(suggest) : 0) + delta) % suggestTags.length
            ]
      );
    },
    [suggest, suggestTags]
  );

  useEffect(() => {
    clearTimeout(suggestTimeout.current);
    suggestTimeout.current = window.setTimeout(async () => {
      const id = suggestTimeout.current;
      const client = createApiClient();

      if (client && suggestQuery.trim()) {
        setLoading(true);

        try {
          const result = await client.book.suggestBooks({
            suggestQuery: {
              fuzzy: true,
              limit: 50,
              prefix: suggestQuery,
            },
          });

          if (id === suggestTimeout.current) {
            let tags: TagSuggestion[] = [];

            for (const key in result.tags) {
              const tag = key as BookTag;

              for (const item of result.tags[tag] || []) {
                tags.push({ ...item, type: "tag", tag });
              }
            }

            tags = sort(tags);

            function sort<T extends SuggestItem>(items: T[]) {
              return items.sort((a, b) => levenshtein(suggestQuery, a.text) - levenshtein(suggestQuery, b.text));
            }

            setSuggest(tags[0]);
            setSuggestText(suggestQuery);
            setSuggestTags(tags);
            setSuggestBooks(sort([...result.primaryName, ...result.englishName].map((x) => ({ ...x, type: "book" }))));
          }

          return;
        } catch (e) {
          console.error(e);
        } finally {
          setLoading(false);
        }
      }

      setSuggest(undefined);
      setSuggestText(suggestQuery);
      setSuggestTags([]);
      setSuggestBooks([]);
    }, 200);
  }, [suggestQuery, setLoading]);

  return (
    <Flex direction="column">
      <Input
        inputRef={inputRef}
        value={display}
        setValue={setDisplay}
        loading={load > 0}
        onSubmit={complete}
        onSuggestChange={moveSuggest}
        onFocus={setInputFocus}
      />

      {display.trim() && (
        <Suggest
          value={suggestText}
          focused={inputFocus ? suggest : undefined}
          onSelected={complete}
          tags={suggestTags}
          books={suggestBooks}
          setLoading={setLoading}
        />
      )}
    </Flex>
  );
};

export default memo(Content);
