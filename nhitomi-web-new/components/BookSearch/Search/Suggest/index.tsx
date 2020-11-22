import React, { Dispatch, memo, useLayoutEffect, useMemo, useRef, useState } from "react";
import TagItem from "./TagItem";
import { Box, Divider } from "@chakra-ui/react";
import { BookSuggestion, TagSuggestion } from "../Content";
import BookItem from "./BookItem";
import { Book } from "nhitomi-api";
import { createApiClient } from "../../../../utils/client";

const Suggest = ({
  value,
  focused,
  onSelected,
  tags,
  books,
  setLoading,
}: {
  value: string;
  focused?: TagSuggestion;
  onSelected: Dispatch<TagSuggestion>;
  tags: TagSuggestion[];
  books: BookSuggestion[];
  setLoading: Dispatch<boolean>;
}) => {
  const loadId = useRef<number>(0);
  const [bookItems, setBookItems] = useState<Book[]>([]);

  useLayoutEffect(() => {
    setBookItems([]);

    (async () => {
      const id = ++loadId.current;
      setLoading(true);

      try {
        const client = createApiClient();

        if (client && books.length) {
          const items = await client.book.getBooks({
            getBookManyRequest: {
              ids: books.slice(0, 6).map((book) => book.id),
            },
          });

          if (id === loadId.current) {
            setBookItems(items);
          }

          return;
        }
      } catch (e) {
        console.error(e);
      } finally {
        setLoading(false);
      }

      setBookItems([]);
    })();
  }, [books, setLoading]);

  return (
    <>
      {useMemo(
        () =>
          !!tags.length && (
            <Box p={2} overflow="hidden">
              {tags.map((item) => (
                <TagItem
                  key={item.id}
                  focused={focused === item}
                  onClick={() => onSelected(item)}
                  comparand={value}
                  text={item.text}
                  tag={item.tag}
                />
              ))}
            </Box>
          ),
        [focused, onSelected, tags, value]
      )}

      {tags.length && bookItems.length ? <Divider /> : null}

      {useMemo(
        () =>
          !!bookItems.length && (
            <Box p={2}>
              {bookItems.map((book) => (
                <BookItem key={book.id} book={book} />
              ))}
            </Box>
          ),
        [bookItems]
      )}
    </>
  );
};

export default memo(Suggest);
