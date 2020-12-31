import React, { Dispatch, memo } from "react";
import { FormControl, FormLabel, Textarea } from "@chakra-ui/react";
import { useT } from "../../../locales";

const DescriptionInput = ({ value, setValue }: { value: string; setValue: Dispatch<string> }) => {
  const t = useT();

  return (
    <FormControl id="description">
      <FormLabel>{t("CollectionEditor.InfoPanel.DescriptionInput.label")}</FormLabel>
      <Textarea value={value} onChange={({ currentTarget: { value } }) => setValue(value)} />
    </FormControl>
  );
};

export default memo(DescriptionInput);
