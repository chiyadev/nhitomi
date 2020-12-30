import React, { Dispatch, memo, RefObject, useCallback, useEffect, useMemo, useState } from "react";
import { Collection } from "nhitomi-api";
import { createApiClient } from "../../utils/client";
import {
  Button,
  Center,
  DrawerBody,
  DrawerCloseButton,
  DrawerFooter,
  DrawerHeader,
  Fade,
  Icon,
  Input,
  InputGroup,
  InputLeftElement,
  Spinner,
  VStack,
} from "@chakra-ui/react";
import { useT } from "../../locales";
import ItemDisplay from "./ItemDisplay";
import { FaPlus, FaSearch } from "react-icons/fa";
import CollectionCreator from "../CollectionCreator";
import { useErrorToast } from "../../utils/hooks";
import escapeStringRegexp from "escape-string-regexp";
import { captureException } from "@sentry/minimal";

const Content = ({
  focusRef,
  userId,
  onSelect,
  onFilter,
}: {
  focusRef: RefObject<HTMLInputElement>;
  userId: string;
  onSelect?: Dispatch<Collection>;
  onFilter?: (collection: Collection) => boolean;
}) => {
  const t = useT();
  const error = useErrorToast();
  const [create, setCreate] = useState(false);
  const [search, setSearch] = useState("");
  const [collections, setCollections] = useState<Collection[]>();

  useEffect(() => {
    (async () => {
      const client = createApiClient();

      try {
        const { items } = await client?.user.getUserCollections({
          id: userId,
        });

        setCollections(items);
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
          <div>{t("CollectionSelector.title")}</div>

          <InputGroup>
            <InputLeftElement pointerEvents="none">
              <Icon as={FaSearch} color="gray.500" />
            </InputLeftElement>

            <Input
              ref={focusRef}
              value={search}
              onChange={({ currentTarget: { value } }) => setSearch(value)}
              placeholder={t("CollectionSelector.search")}
            />
          </InputGroup>
        </VStack>
      </DrawerHeader>

      <DrawerBody>
        {useMemo(() => {
          if (collections?.length) {
            const filter = new RegExp(escapeStringRegexp(search), "gi");
            const filtered = collections.filter(
              (collection) => collection.name.match(filter) || (collection.description || "").match(filter)
            );

            return (
              <Fade in>
                <VStack align="stretch" spacing={2}>
                  {filtered.map((collection) => (
                    <ItemDisplay
                      key={collection.id}
                      collection={collection}
                      disabled={onFilter ? !onFilter(collection) : false}
                      onSelect={onSelect}
                    />
                  ))}
                </VStack>
              </Fade>
            );
          } else if (collections) {
            return null;
          } else {
            return (
              <Center>
                <Spinner />
              </Center>
            );
          }
        }, [collections, search])}
      </DrawerBody>

      <CollectionCreator
        open={create}
        setOpen={setCreate}
        onCreate={useCallback(
          (collection) => {
            setCreate(false);
            onSelect?.(collection);
          },
          [onSelect]
        )}
      />

      <DrawerFooter>
        <Button leftIcon={<Icon as={FaPlus} />} onClick={() => setCreate(true)}>
          {t("CollectionSelector.create")}
        </Button>
      </DrawerFooter>
    </>
  );
};

export default memo(Content);
