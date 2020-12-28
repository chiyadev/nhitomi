import React, { memo, useMemo } from "react";
import { Book, Collection } from "nhitomi-api";
import { Grid as GridCore } from "@chakra-ui/react";
import styles from "../../BookGrid/index.module.css";
import Item from "./Item";

const Grid = ({ items }: { items: { cover?: Book; collection: Collection }[] }) => {
  return (
    <GridCore p={2} gap={2} className={styles.grid}>
      {useMemo(
        () => items.map(({ cover, collection }) => <Item key={collection.id} cover={cover} collection={collection} />),
        [items]
      )}
    </GridCore>
  );
};

export default memo(Grid);
