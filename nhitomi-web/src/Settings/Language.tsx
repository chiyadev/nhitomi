import React, { useMemo } from "react";
import { FormattedMessage } from "react-intl";
import { SettingsFocusContainer } from "./SettingsFocusContainer";
import { CheckBox } from "../Components/Checkbox";
import { AvailableLocalizations } from "../Languages/languages";
import { LanguageNames } from "../LocaleManager";
import { useConfig } from "../ConfigManager";
import { LocaleFlag } from "../Components/LocaleFlag";
import { LanguageTypes } from "../orderedConstants";
import { Disableable } from "../Components/Disableable";

export const Language = () => {
  const [interfaceLanguage, setInterfaceLanguage] = useConfig("language");
  const [searchLanguages, setSearchLanguages] = useConfig("searchLanguages");

  return (
    <SettingsFocusContainer focus="language">
      <div>
        <FormattedMessage id="pages.settings.appearance.language.name" />
      </div>
      <div className="text-sm text-gray-darker">
        <FormattedMessage id="pages.settings.appearance.language.description" />
      </div>
      <br />

      <div>
        <div>
          <FormattedMessage id="pages.settings.appearance.language.interface" />
        </div>

        {useMemo(
          () =>
            LanguageTypes.filter(
              (l) => AvailableLocalizations.indexOf(l) !== -1
            ).map((language) => (
              <CheckBox
                type="radio"
                value={language === interfaceLanguage}
                setValue={(v) => {
                  if (v) setInterfaceLanguage(language);
                }}
              >
                <span>
                  <LocaleFlag language={language} size={20} />{" "}
                  {LanguageNames[language]}
                </span>
              </CheckBox>
            )),
          [interfaceLanguage, setInterfaceLanguage]
        )}
      </div>
      <br />

      <div>
        <div>
          <FormattedMessage id="pages.settings.appearance.language.search" />
        </div>

        {useMemo(
          () =>
            LanguageTypes.map((language) => (
              <Disableable
                disabled={
                  searchLanguages.length === 1 &&
                  searchLanguages[0] === language
                }
              >
                <CheckBox
                  value={searchLanguages.indexOf(language) !== -1}
                  setValue={(v) => {
                    if (v)
                      setSearchLanguages(
                        [...searchLanguages, language].filter(
                          (v, i, a) => a.indexOf(v) === i
                        )
                      );
                    else
                      setSearchLanguages(
                        searchLanguages.filter((l) => l !== language)
                      );
                  }}
                >
                  <span>
                    <LocaleFlag language={language} size={20} />{" "}
                    {LanguageNames[language]}
                  </span>
                </CheckBox>
              </Disableable>
            )),
          [searchLanguages, setSearchLanguages]
        )}
      </div>
    </SettingsFocusContainer>
  );
};
