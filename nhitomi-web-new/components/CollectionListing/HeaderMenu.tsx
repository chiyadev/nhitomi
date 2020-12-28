import React, { memo, useState } from "react";
import { useT } from "../../locales";
import CollectionCreator from "../CollectionCreator";
import { useRouter } from "next/router";
import ButtonItem from "../Header/ButtonItem";
import { FaPlus } from "react-icons/fa";

const HeaderMenu = () => {
  const t = useT();
  const router = useRouter();
  const [create, setCreate] = useState(false);

  return (
    <>
      <CollectionCreator
        open={create}
        setOpen={setCreate}
        onCreate={async (collection) => {
          setCreate(false);

          await router.push(`/collections/${collection.id}`);
        }}
      />

      <ButtonItem name={t("CollectionListing.HeaderMenu.create")} icon={FaPlus} onClick={() => setCreate(true)} />
    </>
  );
};

export default memo(HeaderMenu);
