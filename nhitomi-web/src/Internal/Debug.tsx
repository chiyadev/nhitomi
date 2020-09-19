import React from "react";
import { useAlert, useNotify } from "../NotificationManager";
import { FlatButton } from "../Components/FlatButton";
import { Container } from "../Components/Container";
import { useTabTitle } from "../TitleSetter";
import { colors } from "../theme.json";
import { ColorHue, ColorLuminance, getColor } from "../theme";

export const Debug = () => {
  useTabTitle("Debug helper");

  return (
    <Container className='space-y-4'>
      <Notifications />
      <Alerts />
      <Colors />
    </Container>
  );
};

const Notifications = () => {
  const { notify, notifyError } = useNotify();

  return (
    <div>
      <div>Notifications</div>
      <div className='space-x-1'>
        <FlatButton onClick={() => notify("info", "message", "description")}>Notify info</FlatButton>
        <FlatButton onClick={() => notify("success", "message", "description")}>Notify success</FlatButton>
        <FlatButton onClick={() => notify("warning", "message", "description")}>Notify warning</FlatButton>
        <FlatButton onClick={() => notifyError(Error("notify error"))}>Notify error</FlatButton>
      </div>
    </div>
  );
};

const Alerts = () => {
  const { alert } = useAlert();

  return (
    <div>
      <div>Alerts</div>
      <div className='space-x-1'>
        <FlatButton onClick={() => alert("message")}>Alert</FlatButton>
        <FlatButton onClick={() => alert("message", "info")}>Alert info</FlatButton>
        <FlatButton onClick={() => alert("message", "success")}>Alert success</FlatButton>
        <FlatButton onClick={() => alert("message", "warning")}>Alert warning</FlatButton>
        <FlatButton onClick={() => alert("message", "error")}>Alert error</FlatButton>
      </div>
    </div>
  );
};

const Colors = () => {
  const luminances: ColorLuminance[] = ["lightest", "lighter", "default", "darker", "darkest"];

  return (
    <div>
      <div>Colors</div>
      <div className='space-y-1'>
        {Object.keys(colors).map(key => (
          <div className='flex flex-row space-x-1'>
            {luminances.map(luminance => (
              <div
                className='flex-1 h-12 text-xs overflow-hidden'
                style={{ backgroundColor: getColor(key as ColorHue, luminance).rgb }}>

                <div>{key} {luminance}</div>
                <div>{getColor(key as ColorHue, luminance).hex}</div>
              </div>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
};
