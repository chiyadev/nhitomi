import React, { Dispatch, memo, RefObject, useState } from "react";
import {
  Button,
  ButtonGroup,
  FormControl,
  FormLabel,
  Icon,
  Input,
  ModalBody,
  ModalCloseButton,
  ModalFooter,
  ModalHeader,
  Select,
  Textarea,
  VStack,
} from "@chakra-ui/react";
import { useT } from "../../locales";
import { FaCheck } from "react-icons/fa";
import { createApiClient } from "../../utils/client";
import { Collection, ObjectType } from "nhitomi-api";
import { useErrorToast } from "../../utils/hooks";

const Content = ({
  focusRef,
  setOpen,
  onCreate,
}: {
  focusRef: RefObject<HTMLInputElement>;
  setOpen: Dispatch<boolean>;
  onCreate?: Dispatch<Collection>;
}) => {
  const t = useT();
  const error = useErrorToast();
  const [load, setLoad] = useState(false);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [type, setType] = useState(ObjectType.Book);

  return (
    <>
      <ModalCloseButton />
      <ModalHeader>{t("CollectionCreator.title")}</ModalHeader>

      <form
        onSubmit={async (e) => {
          e.preventDefault();
          setLoad(true);

          try {
            const client = createApiClient();

            const collection = await client.collection.createCollection({
              createCollectionRequest: {
                type,
                collection: {
                  name,
                  description,
                },
              },
            });

            onCreate?.(collection);
          } catch (e) {
            console.error(e);
            error(e);
          } finally {
            setLoad(false);
          }
        }}
      >
        <ModalBody>
          <VStack align="stretch" spacing={4}>
            <FormControl id="name" isRequired>
              <FormLabel>{t("CollectionCreator.name")}</FormLabel>
              <Input ref={focusRef} value={name} onChange={({ currentTarget: { value } }) => setName(value)} />
            </FormControl>

            <FormControl id="description">
              <FormLabel>{t("CollectionCreator.description")}</FormLabel>
              <Textarea value={description} onChange={({ currentTarget: { value } }) => setDescription(value)} />
            </FormControl>

            <FormControl id="type">
              <FormLabel>{t("CollectionCreator.type")}</FormLabel>
              <Select value={type} onChange={({ currentTarget: { value } }) => setType(value as ObjectType)}>
                {Object.values(ObjectType).map((type) => (
                  <option key={type} disabled={type !== ObjectType.Book}>
                    {t("ObjectType", { value: type })}
                  </option>
                ))}
              </Select>
            </FormControl>
          </VStack>
        </ModalBody>

        <ModalFooter>
          <ButtonGroup>
            <Button type="submit" colorScheme="blue" leftIcon={<Icon as={FaCheck} />} isLoading={load}>
              {t("CollectionCreator.create")}
            </Button>

            <Button onClick={() => setOpen(false)} disabled={load}>
              {t("CollectionCreator.cancel")}
            </Button>
          </ButtonGroup>
        </ModalFooter>
      </form>
    </>
  );
};

export default memo(Content);
