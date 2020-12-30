import React, { Dispatch, memo, Ref, useEffect, useMemo, useState } from "react";
import { Book, Collection } from "nhitomi-api";
import {
  Button,
  ButtonGroup,
  Center,
  Checkbox,
  CheckboxGroup,
  Divider,
  DrawerBody,
  DrawerCloseButton,
  DrawerFooter,
  DrawerHeader,
  HStack,
  Icon,
  Input,
  InputGroup,
  InputLeftElement,
  Spinner,
  VStack,
} from "@chakra-ui/react";
import { useT } from "../../../locales";
import { FaCheck, FaSearch } from "react-icons/fa";
import { useErrorToast } from "../../../utils/hooks";
import { createApiClient } from "../../../utils/client";
import { QueryChunkSize } from "../../../utils/constants";
import escapeStringRegexp from "escape-string-regexp";
import { trackEvent } from "../../../utils/umami";
import { captureException } from "@sentry/minimal";

const Content = ({
  focusRef,
  collection,
  setOpen,
  onSelect,
}: {
  focusRef: Ref<HTMLInputElement>;
  collection: Collection;
  setOpen: Dispatch<boolean>;
  onSelect?: Dispatch<Book[]>;
}) => {
  const t = useT();
  const error = useErrorToast();
  const [search, setSearch] = useState("");
  const [books, setItems] = useState<Book[]>();
  const [selected, setSelected] = useState<string[]>([]);

  useEffect(() => {
    if (!collection.items.length) {
      setItems([]);
    }

    (async () => {
      try {
        const client = createApiClient();
        const items: Book[] = [];

        for (let i = 0; i < collection.items.length; i += QueryChunkSize) {
          const chunk = await client.book.getBooks({
            getBookManyRequest: {
              ids: collection.items.slice(i, i + QueryChunkSize),
            },
          });

          for (const item of chunk) {
            items.push(item);
          }

          setItems(items);
        }
      } catch (e) {
        captureException(e);
        error(e);
      }
    })();
  }, []);

  return (
    <>
      <DrawerCloseButton />
      <DrawerHeader>
        <VStack align="stretch" spacing={4}>
          <HStack spacing={2}>
            <div>{t("CollectionItemSelector.Book.title")}</div>
            {books && <div>({selected.length})</div>}
          </HStack>

          <InputGroup>
            <InputLeftElement pointerEvents="none">
              <Icon as={FaSearch} color="gray.500" />
            </InputLeftElement>

            <Input
              ref={focusRef}
              value={search}
              onChange={({ currentTarget: { value } }) => setSearch(value)}
              placeholder={t("CollectionItemSelector.Book.search")}
            />
          </InputGroup>
        </VStack>
      </DrawerHeader>

      <DrawerBody>
        {useMemo(() => {
          if (books?.length) {
            const filter = new RegExp(escapeStringRegexp(search), "gi");
            const filtered = books.filter(
              (book) => book.id.match(filter) || book.primaryName.match(filter) || book.englishName?.match(filter)
            );

            const selectedSet = new Set(selected);
            const filteredIds = filtered.map(({ id }) => id);

            return (
              <VStack align="start" spacing={4} divider={<Divider />}>
                <Checkbox
                  isDisabled={!filtered.length}
                  isChecked={!!filtered.length && filteredIds.every((id) => selectedSet.has(id))}
                  isIndeterminate={
                    filteredIds.some((id) => selectedSet.has(id)) && !filteredIds.every((id) => selectedSet.has(id))
                  }
                  onChange={({ currentTarget: { checked, indeterminate } }) => {
                    const set = new Set(selected);

                    if (checked || indeterminate) {
                      for (const id of filteredIds) {
                        set.add(id);
                      }

                      setSelected(Array.from(set));
                    } else {
                      for (const id of filteredIds) {
                        set.delete(id);
                      }

                      setSelected(Array.from(set));
                    }
                  }}
                >
                  {t("CollectionItemSelector.Book.selectAll")}
                </Checkbox>

                {filtered.length && (
                  <CheckboxGroup value={selected} onChange={setSelected as any}>
                    <VStack align="start" spacing={2}>
                      {filtered.map((book) => (
                        <Checkbox key={book.id} value={book.id}>
                          {book.primaryName}
                        </Checkbox>
                      ))}
                    </VStack>
                  </CheckboxGroup>
                )}
              </VStack>
            );
          } else if (books) {
            return null;
          } else {
            return (
              <Center>
                <Spinner />
              </Center>
            );
          }
        }, [books, selected, search])}
      </DrawerBody>

      <DrawerFooter>
        <ButtonGroup>
          {selected.length ? (
            <Button
              leftIcon={<Icon as={FaCheck} />}
              colorScheme="blue"
              onClick={() => {
                const ids = new Set(selected);
                onSelect?.((books || []).filter(({ id }) => ids.has(id)));

                trackEvent("collectionBookSelector", "select");
              }}
            >
              {t("CollectionItemSelector.Book.select")}
            </Button>
          ) : null}

          <Button onClick={() => setOpen(false)}>{t("CollectionItemSelector.Book.cancel")}</Button>
        </ButtonGroup>
      </DrawerFooter>
    </>
  );
};

export default memo(Content);
