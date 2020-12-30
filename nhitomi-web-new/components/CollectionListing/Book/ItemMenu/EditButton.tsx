import React, { memo, useState } from "react";
import { useT } from "../../../../locales";
import ElementPortal from "../../../ElementPortal";
import { Icon, MenuItem } from "@chakra-ui/react";
import { FaEdit } from "react-icons/fa";
import CollectionEditor from "../../../CollectionEditor";
import { Collection } from "nhitomi-api";
import { useRouter } from "next/router";
import { trackEvent } from "../../../../utils/umami";

const EditButton = ({ collection }: { collection: Collection }) => {
  const t = useT();
  const router = useRouter();
  const [edit, setEdit] = useState(false);

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

      <ElementPortal.Consumer>
        <MenuItem
          icon={<Icon as={FaEdit} />}
          onClick={() => {
            setEdit(true);
            trackEvent("collectionListing", "itemEdit");
          }}
        >
          {t("CollectionListing.Book.ItemMenu.EditButton.text")}
        </MenuItem>
      </ElementPortal.Consumer>
    </>
  );
};

export default memo(EditButton);
