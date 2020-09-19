import React from "react";
import { FormattedMessage } from "react-intl";
import { SettingsFocusContainer } from "./SettingsFocusContainer";
import { BlurSupported, useConfig } from "../ConfigManager";
import { CheckBox } from "../Components/Checkbox";
import { Disableable } from "../Components/Disableable";

export const Blur = () => {
  const [blur, setBlur] = useConfig("blur");

  return (
    <SettingsFocusContainer focus='blur'>
      <div><FormattedMessage id='pages.settings.appearance.blur.name' /></div>
      <div className='text-sm text-gray-darker'><FormattedMessage id='pages.settings.appearance.blur.description' /></div>
      <br />

      <Disableable disabled={!BlurSupported}>
        <CheckBox
          value={blur}
          setValue={setBlur}>

          {blur
            ? <FormattedMessage id='pages.settings.appearance.blur.enabled' />
            : <FormattedMessage id='pages.settings.appearance.blur.disabled' />}
        </CheckBox>
      </Disableable>
    </SettingsFocusContainer>
  );
};
