import React, { memo, useState } from "react";
import ButtonItem from "../Header/ButtonItem";
import { FaSearch, FaSortAmountUp } from "react-icons/fa";
import QueryDrawer from "./QueryDrawer";
import { useT } from "../../locales";
import { trackEvent } from "../../utils/umami";
import BookSearchOverlay from "../BookSearchOverlay";

const HeaderMenu = () => {
  const t = useT();
  const [search, setSearch] = useState(false);
  const [sort, setSort] = useState(false);

  return (
    <>
      <BookSearchOverlay open={search} setOpen={setSearch} />
      <QueryDrawer open={sort} setOpen={setSort} />

      <ButtonItem
        name={t("BookListing.HeaderMenu.search")}
        icon={FaSearch}
        onClick={() => {
          setSearch(true);
          trackEvent("header", "search");
        }}
      />

      <ButtonItem
        name={t("BookListing.HeaderMenu.sort")}
        icon={FaSortAmountUp}
        onClick={() => {
          setSort(true);
          trackEvent("bookListing", "queryDrawer");
        }}
      />
    </>
  );
};

export default memo(HeaderMenu);
