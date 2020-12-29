import React, { memo } from "react";
import { ObjectType } from "nhitomi-api";
import { useT } from "../../../locales";
import { FormControl, FormLabel, Select } from "@chakra-ui/react";

const TypeSelect = ({ value }: { value: ObjectType }) => {
  const t = useT();

  return (
    <FormControl id="type">
      <FormLabel>{t("CollectionEditor.InfoPanel.TypeSelect.label")}</FormLabel>
      <Select value={value} disabled>
        {Object.values(ObjectType).map((type) => (
          <option key={type} value={type}>
            {t("ObjectType", { value: type })}
          </option>
        ))}
      </Select>
    </FormControl>
  );
};

export default memo(TypeSelect);
