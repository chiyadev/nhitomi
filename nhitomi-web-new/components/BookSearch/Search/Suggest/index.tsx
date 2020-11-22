import React, { Dispatch, memo, useLayoutEffect, useMemo, useRef, useState } from "react";
import TagItem from "./TagItem";
import { Divider, Flex, Grid, Kbd, Spacer, Text } from "@chakra-ui/react";
import { BookSuggestion, TagSuggestion } from "../Content";
import { Book } from "nhitomi-api";
import { createApiClient } from "../../../../utils/client";
import Item from "../../../BookGrid/Item";

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
            <Flex direction="column" p={4} overflow="hidden">
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

              {tags.length > 1 && (
                <Flex mt={2} mb={-2}>
                  <Spacer />
                  <Text fontSize={12}>
                    use <Kbd>↑</Kbd>
                    <Kbd>↓</Kbd>
                  </Text>
                </Flex>
              )}
            </Flex>
          ),
        [focused, onSelected, tags, value]
      )}

      {tags.length && bookItems.length ? <Divider /> : null}

      {useMemo(
        () =>
          !!bookItems.length && (
            <Flex direction="column" p={4}>
              <Grid templateColumns="repeat(auto-fill, minmax(8rem, 1fr))" gap={2}>
                {bookItems.map((book) => (
                  <Item key={book.id} book={book} />
                ))}
              </Grid>

              <Flex mt={2} mb={-2}>
                <Spacer />
                <Text fontSize={12}>
                  use <Kbd>tab ↹</Kbd>
                </Text>
              </Flex>
            </Flex>
          ),
        [bookItems]
      )}

      {value && !tags.length && !bookItems.length && (
        <Flex p={2}>
          <Spacer />
          <Text fontSize={12}>
            search <Kbd>enter ↵</Kbd>
          </Text>
        </Flex>
      )}
    </>
  );
};

export default memo(Suggest);
