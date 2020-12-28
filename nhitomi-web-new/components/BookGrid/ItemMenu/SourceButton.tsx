import React, { memo } from "react";
import { Icon, MenuItem } from "@chakra-ui/react";
import { FaExternalLinkAlt } from "react-icons/fa";
import { Book, BookContent } from "nhitomi-api";
import { useT } from "../../../locales";
import ElementPortal from "../../ElementPortal";

const SourceButton = ({ content }: { book: Book; content: BookContent }) => {
  const t = useT();

  return (
    <ElementPortal.Consumer>
      <MenuItem
        as="a"
        href={content.sourceUrl}
        target="_blank"
        rel="noopener noreferrer"
        icon={<Icon as={FaExternalLinkAlt} />}
      >
        {t("BookGrid.ItemMenu.SourceButton.text")}
      </MenuItem>
    </ElementPortal.Consumer>
  );
};

export default memo(SourceButton);
