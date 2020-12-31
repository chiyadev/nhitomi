import React, { Dispatch, memo, Ref, useState } from "react";
import { DrawerCloseButton, DrawerHeader, Tab, TabList, Tabs } from "@chakra-ui/react";
import { useT } from "../../locales";
import { Collection } from "nhitomi-api";
import InfoPanel from "./InfoPanel";

const Content = ({
  focusRef,
  collection,
  setOpen,
  onSubmit,
}: {
  focusRef: Ref<HTMLInputElement>;
  collection: Collection;
  setOpen: Dispatch<boolean>;
  onSubmit?: Dispatch<Collection>;
}) => {
  const t = useT();
  const [current, setCurrent] = useState(collection);
  const [index, setIndex] = useState(0);

  return (
    <>
      <DrawerCloseButton />
      <DrawerHeader>{t("CollectionEditor.title", { collection: collection.name })}</DrawerHeader>

      <Tabs size="sm" px={6} mb={2} index={index} onChange={setIndex}>
        <TabList>
          <Tab>{t("CollectionEditor.infoTab")}</Tab>
        </TabList>
      </Tabs>

      {index === 0 && (
        <InfoPanel
          focusRef={focusRef}
          collection={current}
          setCollection={setCurrent}
          setOpen={setOpen}
          onSubmit={onSubmit}
        />
      )}
    </>
  );
};

export default memo(Content);
