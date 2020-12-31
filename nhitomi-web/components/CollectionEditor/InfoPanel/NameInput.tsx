import React, { Dispatch, memo, Ref } from "react";
import { FormControl, FormLabel, Input } from "@chakra-ui/react";
import { useT } from "../../../locales";

const NameInput = ({
  inputRef,
  value,
  setValue,
}: {
  inputRef: Ref<HTMLInputElement>;
  value: string;
  setValue: Dispatch<string>;
}) => {
  const t = useT();

  return (
    <FormControl id="name" isRequired>
      <FormLabel>{t("CollectionEditor.InfoPanel.NameInput.label")}</FormLabel>
      <Input ref={inputRef} value={value} onChange={({ currentTarget: { value } }) => setValue(value)} />
    </FormControl>
  );
};

export default memo(NameInput);
