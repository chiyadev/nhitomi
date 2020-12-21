import React, { memo, useState } from "react";
import IconItem from "../Header/IconItem";
import { FaSortAmountUp } from "react-icons/fa";
import QueryDrawer from "./QueryDrawer";
import { useT } from "../../locales";

const HeaderMenu = () => {
  const [sort, setSort] = useState(false);
  const t = useT();

  return (
    <>
      <QueryDrawer open={sort} setOpen={setSort} />

      <IconItem name={t("BookListing.HeaderMenu.sort")} icon={FaSortAmountUp} onClick={() => setSort(true)} />
    </>
  );
};

export default memo(HeaderMenu);
