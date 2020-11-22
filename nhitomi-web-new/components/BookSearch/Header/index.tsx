import React, { memo } from "react";
import { Flex, Spacer } from "@chakra-ui/react";
import IconItem from "./IconItem";
import { FaSearch } from "react-icons/fa";

const Header = ({ onSearch }: { onSearch?: () => void }) => {
  return (
    <Flex>
      <Spacer />
      <IconItem name="Search" icon={FaSearch} onClick={onSearch} />
    </Flex>
  );
};

export default memo(Header);
