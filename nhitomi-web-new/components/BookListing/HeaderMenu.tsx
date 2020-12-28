import React, { memo, useState } from "react";
import ButtonItem from "../Header/ButtonItem";
import { FaSortAmountUp } from "react-icons/fa";
import QueryDrawer from "./QueryDrawer";
import { useT } from "../../locales";

const HeaderMenu = () => {
  const t = useT();
  const [sort, setSort] = useState(false);

  return (
    <>
      <QueryDrawer open={sort} setOpen={setSort} />

      <ButtonItem name={t("BookListing.HeaderMenu.sort")} icon={FaSortAmountUp} onClick={() => setSort(true)} />
    </>
  );
};

export default memo(HeaderMenu);
