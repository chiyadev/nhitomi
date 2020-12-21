import React, { memo, ReactNode, useState } from "react";
import { chakra, HStack } from "@chakra-ui/react";
import IconItem from "./IconItem";
import { FaSearch } from "react-icons/fa";
import SearchOverlay from "./SearchOverlay";
import LayoutBody from "../LayoutBody";
import StickyOverlay from "./StickyOverlay";
import { useT } from "../../locales";

const Header = ({ title, menu, shadow }: { title?: ReactNode; menu?: ReactNode; shadow?: boolean }) => {
  const t = useT();
  const [search, setSearch] = useState(false);

  return (
    <>
      <SearchOverlay open={search} setOpen={setSearch} />

      <StickyOverlay shadow={shadow}>
        <LayoutBody p={4}>
          <HStack spacing={4} minH={6}>
            <chakra.div flex={1} minW={0}>
              {title}
            </chakra.div>

            {menu}

            <IconItem name={t("Header.search")} icon={FaSearch} onClick={() => setSearch(true)} />
          </HStack>
        </LayoutBody>
      </StickyOverlay>
    </>
  );
};

export default memo(Header);
