import React, { memo, useState } from "react";
import ElementPortal from "../../../ElementPortal";
import { Icon, MenuItem } from "@chakra-ui/react";
import { FaTrash } from "react-icons/fa";
import { useT } from "../../../../locales";
import { Collection } from "nhitomi-api";
import CollectionDeleter from "../../../CollectionDeleter";
import { useRouter } from "next/router";

const DeleteButton = ({ collection }: { collection: Collection }) => {
  const t = useT();
  const router = useRouter();
  const [confirm, setConfirm] = useState(false);

  return (
    <>
      <CollectionDeleter
        open={confirm}
        setOpen={setConfirm}
        collection={collection}
        onDelete={() => {
          setConfirm(false);
          router.reload();
        }}
      />

      <ElementPortal.Consumer>
        <MenuItem color="red.300" icon={<Icon as={FaTrash} />} onClick={() => setConfirm(true)}>
          {t("CollectionListing.Book.ItemMenu.DeleteButton.text")}
        </MenuItem>
      </ElementPortal.Consumer>
    </>
  );
};

export default memo(DeleteButton);
