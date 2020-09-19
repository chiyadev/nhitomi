import React from "react";
import { animated, useSpring } from "react-spring";
import { useClientInfo } from "./ClientManager";
import { FormattedMessage } from "react-intl";
import { WarningFilled } from "@ant-design/icons";
import { Container } from "./Components/Container";

export const MaintenanceHeader = () => {
  const { info: { maintenance } } = useClientInfo();

  if (!maintenance)
    return null;

  return (
    <Inner />
  );
};

const Inner = () => {
  const style = useSpring({
    from: { marginTop: -5, opacity: 0 },
    to: { marginTop: 0, opacity: 1 }
  });

  return (
    <Container>
      <animated.div
        style={style}
        className='w-full px-4 py-2 text-sm bg-red-darkest text-white rounded-b'>

        <WarningFilled />
        {" "}
        <FormattedMessage id='components.maintenanceHeader.text' />
      </animated.div>
    </Container>
  );
};
