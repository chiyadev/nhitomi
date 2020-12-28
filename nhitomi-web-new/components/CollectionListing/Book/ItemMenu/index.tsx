import React, { Dispatch, memo, ReactNode, useContext, useMemo } from "react";
import { MenuDivider } from "@chakra-ui/react";
import { Collection } from "nhitomi-api";
import { CollectionMenuContext } from "../..";
import DeleteButton from "./DeleteButton";
import ElementPortal from "../../../ElementPortal";

const ItemMenu = ({ collection, setMenu }: { collection: Collection; setMenu: Dispatch<ReactNode> }) => {
  const { render } = useContext(CollectionMenuContext);
  const additional = useMemo(() => render(collection), [render, collection]);

  return (
    <ElementPortal.Provider onRender={setMenu}>
      <DeleteButton collection={collection} />

      {additional && (
        <>
          <MenuDivider />
          {additional}
        </>
      )}
    </ElementPortal.Provider>
  );
};

export default memo(ItemMenu);
