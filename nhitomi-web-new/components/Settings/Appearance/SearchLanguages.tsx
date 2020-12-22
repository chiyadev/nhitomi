import React, { memo } from "react";
import { useConfig } from "../../../utils/config";
import SectionItem from "../SectionItem";
import { useT } from "../../../locales";
import { Checkbox, CheckboxGroup } from "@chakra-ui/checkbox";
import { VStack } from "@chakra-ui/layout";
import { LanguageTypes } from "../../../utils/constants";
import { HStack } from "@chakra-ui/react";
import LanguageFlag from "../../LanguageFlag";
import { LanguageType } from "nhitomi-api";

const SearchLanguages = () => {
  const t = useT();
  const [languages, setLanguages] = useConfig("searchLanguages");

  return (
    <SectionItem
      title={t("Settings.Appearance.SearchLanguages.title")}
      description={t("Settings.Appearance.SearchLanguages.description")}
    >
      <CheckboxGroup value={languages} onChange={(value) => setLanguages(value as LanguageType[])}>
        <VStack align="start" spacing={2}>
          {LanguageTypes.map((language) => (
            <Checkbox key={language} value={language}>
              <HStack spacing={2}>
                <LanguageFlag language={language} />
                <div>{t("LanguageType", { value: language.replace("-", "") })}</div>
              </HStack>
            </Checkbox>
          ))}
        </VStack>
      </CheckboxGroup>
    </SectionItem>
  );
};

export default memo(SearchLanguages);
