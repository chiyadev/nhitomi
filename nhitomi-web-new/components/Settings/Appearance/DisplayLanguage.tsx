import React, { memo, useState } from "react";
import { createApiClient, useClientInfoAuth } from "../../../utils/client";
import SectionItem from "../SectionItem";
import { HStack, Radio, RadioGroup, VStack } from "@chakra-ui/react";
import { AvailableLocalizations, useT } from "../../../locales";
import BlockingSpinner from "../../BlockingSpinner";
import { LanguageType } from "nhitomi-api";
import { useRouter } from "next/router";
import { useErrorToast } from "../../../utils/hooks";
import LanguageFlag from "../../LanguageFlag";
import { useConfig } from "../../../utils/config";

function getLanguageOrDefault(language?: LanguageType) {
  return language && AvailableLocalizations.includes(language) ? language : LanguageType.EnUs;
}

const DisplayLanguage = () => {
  const t = useT();
  const info = useClientInfoAuth();
  const error = useErrorToast();
  const router = useRouter();
  const [, setSearchLanguages] = useConfig("searchLanguages");
  const [load, setLoad] = useState(false);

  return (
    <SectionItem
      title={t("Settings.Appearance.DisplayLanguage.title")}
      description={t("Settings.Appearance.DisplayLanguage.description")}
    >
      <BlockingSpinner visible={load} />

      <RadioGroup
        value={getLanguageOrDefault(info?.user.language)}
        onChange={async (value) => {
          setLoad(true);

          try {
            const client = createApiClient();
            const language = value as LanguageType;
            const user = await client.user.getSelfUser();

            await client.user.updateUser({
              id: user.id,
              userBase: {
                ...user,
                language,
              },
            });

            setSearchLanguages((l) => l.concat(language).filter((v, i, a) => a.indexOf(v) === i));
            router.reload();
          } catch (e) {
            console.error(e);
            error(e);

            setLoad(false);
          }
        }}
      >
        <VStack align="start" spacing={2}>
          {AvailableLocalizations.map((language) => (
            <Radio key={language} value={language}>
              <HStack spacing={2}>
                <LanguageFlag language={language} />
                <div>{t("LanguageType", { value: language.replace("-", "") })}</div>
              </HStack>
            </Radio>
          ))}
        </VStack>
      </RadioGroup>
    </SectionItem>
  );
};

export default memo(DisplayLanguage);
