import React, { memo } from "react";
import { Collection } from "nhitomi-api";
import { HStack, Icon, VStack } from "@chakra-ui/react";
import { FaEdit, FaFolderOpen, FaHistory } from "react-icons/fa";
import { useT } from "../../../locales";
import DateDisplay from "../../DateDisplay";

const InfoText = ({ collection }: { collection: Collection }) => {
  const t = useT();

  return (
    <VStack align="start" fontSize="sm" spacing={0}>
      <HStack spacing={2}>
        <Icon as={FaHistory} />
        <div>
          {t("CollectionEditor.InfoPanel.InfoText.createdTime", {
            time: <DateDisplay date={collection.createdTime} />,
          })}
        </div>
      </HStack>

      <HStack spacing={2}>
        <Icon as={FaEdit} />
        <div>
          {t("CollectionEditor.InfoPanel.InfoText.updatedTime", {
            time: <DateDisplay date={collection.updatedTime} />,
          })}
        </div>
      </HStack>

      <HStack spacing={2}>
        <Icon as={FaFolderOpen} />
        <div>
          {t("CollectionEditor.InfoPanel.InfoText.itemCount", {
            count: collection.items.length,
          })}
        </div>
      </HStack>
    </VStack>
  );
};

export default memo(InfoText);
