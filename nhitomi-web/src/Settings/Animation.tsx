import React from "react";
import { FormattedMessage } from "react-intl";
import { SettingsFocusContainer } from "./SettingsFocusContainer";
import { AnimationMode, useConfig } from "../ConfigManager";
import { CheckBox } from "../Components/Checkbox";

export const Animation = () => {
  return (
    <SettingsFocusContainer focus="animation">
      <div>
        <FormattedMessage id="pages.settings.appearance.animation.name" />
      </div>
      <div className="text-sm text-gray-darker">
        <FormattedMessage id="pages.settings.appearance.animation.description" />
      </div>
      <br />

      <div className="space-y-2">
        <Item mode="normal" />
        <Item mode="faster" />
        <Item mode="none" />
      </div>
    </SettingsFocusContainer>
  );
};

const Item = ({ mode }: { mode: AnimationMode }) => {
  const [value, setValue] = useConfig("animation");

  return (
    <CheckBox
      type="radio"
      value={value === mode}
      setValue={(v) => {
        if (v) setValue(mode);
      }}
    >
      <div>
        <FormattedMessage id={`pages.settings.appearance.animation.${mode}.name`} />
      </div>
      <div className="text-sm text-gray-darker">
        <FormattedMessage id={`pages.settings.appearance.animation.${mode}.description`} />
      </div>
    </CheckBox>
  );
};
