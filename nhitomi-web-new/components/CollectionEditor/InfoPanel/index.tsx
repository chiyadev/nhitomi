import React, { Dispatch, memo, Ref, SetStateAction, useState } from "react";
import { Collection } from "nhitomi-api";
import { Button, ButtonGroup, chakra, DrawerBody, DrawerFooter, Icon, VStack } from "@chakra-ui/react";
import NameInput from "./NameInput";
import { usePropertyDispatch } from "../../../utils/dispatch";
import DescriptionInput from "./DescriptionInput";
import InfoText from "./InfoText";
import { createApiClient } from "../../../utils/client";
import { FaCheck } from "react-icons/fa";
import { useErrorToast } from "../../../utils/hooks";
import { useT } from "../../../locales";
import TypeSelect from "./TypeSelect";

const InfoPanel = ({
  focusRef,
  collection,
  setCollection,
  setOpen,
  onSubmit,
}: {
  focusRef: Ref<HTMLInputElement>;
  collection: Collection;
  setCollection: Dispatch<SetStateAction<Collection>>;
  setOpen: Dispatch<boolean>;
  onSubmit?: Dispatch<Collection>;
}) => {
  const t = useT();
  const error = useErrorToast();
  const [load, setLoad] = useState(false);

  const [name, setName] = usePropertyDispatch(collection, setCollection, "name");
  const [description, setDescription] = usePropertyDispatch(collection, setCollection, "description");
  const [type] = usePropertyDispatch(collection, setCollection, "type");

  return (
    <chakra.form
      flex={1}
      display="flex"
      flexDirection="column"
      onSubmit={async (e) => {
        e.preventDefault();
        setLoad(true);

        try {
          const client = createApiClient();

          const updated = await client.collection.updateCollection({
            id: collection.id,
            collectionBase: collection,
          });

          onSubmit?.(updated);
        } catch (e) {
          console.error(e);
          error(e);
        } finally {
          setLoad(false);
        }
      }}
    >
      <DrawerBody>
        <VStack align="stretch" spacing={4}>
          <NameInput inputRef={focusRef} value={name} setValue={setName} />
          <DescriptionInput value={description || ""} setValue={setDescription} />
          <TypeSelect value={type} />
          <InfoText collection={collection} />
        </VStack>
      </DrawerBody>

      <DrawerFooter>
        <ButtonGroup>
          <Button type="submit" colorScheme="blue" leftIcon={<Icon as={FaCheck} />} isLoading={load}>
            {t("CollectionEditor.InfoPanel.save")}
          </Button>

          <Button onClick={() => setOpen(false)} disabled={load}>
            {t("CollectionEditor.InfoPanel.cancel")}
          </Button>
        </ButtonGroup>
      </DrawerFooter>
    </chakra.form>
  );
};

export default memo(InfoPanel);
