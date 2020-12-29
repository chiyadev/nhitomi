import React, { memo, useState } from "react";
import CollectionEditor from "../../CollectionEditor";
import ButtonItem from "../../Header/ButtonItem";
import { FaDownload, FaEdit, FaRandom, FaTrash } from "react-icons/fa";
import { useT } from "../../../locales";
import { Collection } from "nhitomi-api";
import { useRouter } from "next/router";
import CollectionDeleter from "../../CollectionDeleter";
import CollectionDownloader from "./CollectionDownloader";
import { useClientInfoAuth } from "../../../utils/client";
import LinkItem from "../../Header/LinkItem";

const HeaderMenu = ({ collection }: { collection: Collection }) => {
  const t = useT();
  const info = useClientInfoAuth();
  const router = useRouter();
  const [edit, setEdit] = useState(false);
  const [download, setDownload] = useState(false);
  const [delet, setDelete] = useState(false);

  return (
    <>
      <CollectionEditor
        open={edit}
        setOpen={setEdit}
        collection={collection}
        onSubmit={() => {
          setEdit(false);
          router.reload();
        }}
      />

      <ButtonItem name={t("CollectionViewer.HeaderMenu.edit")} icon={FaEdit} onClick={() => setEdit(true)} />

      <ButtonItem
        name={t("CollectionViewer.HeaderMenu.random")}
        icon={FaRandom}
        onClick={async () => {
          const id = collection.items[Math.floor(Math.random() * collection.items.length)];

          await router.push(`/books/${id}`);
        }}
      />

      {info?.user.isSupporter ? (
        <>
          <CollectionDownloader open={download} setOpen={setDownload} collection={collection} />

          <ButtonItem
            name={t("CollectionViewer.HeaderMenu.download")}
            icon={FaDownload}
            onClick={() => setDownload(true)}
          />
        </>
      ) : (
        <LinkItem name={t("CollectionViewer.HeaderMenu.download")} icon={FaDownload} href="/support" />
      )}

      <CollectionDeleter
        open={delet}
        setOpen={setDelete}
        collection={collection}
        onDelete={async () => {
          setDelete(false);
          await router.push(`/users/${collection.ownerIds[0]}/collections`);
        }}
      />

      <ButtonItem name={t("CollectionViewer.HeaderMenu.delete")} icon={FaTrash} onClick={() => setDelete(true)} />
    </>
  );
};

export default memo(HeaderMenu);
