import React from "react";
import { Container } from "./Components/Container";
import { ExclamationCircleOutlined } from "@ant-design/icons";
import { animated, useSpring } from "react-spring";

import chino500 from "./assets/statuses/chino500.png";

export const Maintenance = ({ error }: { error: Error }) => {
  const style = useSpring({
    from: { opacity: 0 },
    to: { opacity: 1 },
  });

  return (
    <animated.div style={style}>
      <Container className="p-4 space-y-8 text-center">
        <div className="text-xl">
          <ExclamationCircleOutlined /> Maintenance
        </div>

        <img alt="503" src={chino500} className="pointer-events-none mx-auto w-64 h-64 object-cover" />

        <div className="space-y-4 text-sm">
          <div>We will be back in a few minutes!</div>

          <div className="text-gray-darker flex justify-center">
            <code className="whitespace-pre-wrap text-left max-w-lg">{error.stack}</code>
          </div>
        </div>
      </Container>
    </animated.div>
  );
};
