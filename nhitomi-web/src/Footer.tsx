import React, { ReactNode, useMemo, useRef, useState } from "react";
import { Container } from "./Components/Container";
import { animated, useSpring } from "react-spring";
import { getColor } from "./theme";
import { useClientInfo } from "./ClientManager";
import { Tooltip } from "./Components/Tooltip";
import { FormattedMessage } from "react-intl";
import { Anchor } from "./Components/Anchor";
import { HeartFilled } from "@ant-design/icons";
import { SupportLink } from "./Support";
import { useConfig } from "./ConfigManager";

export const Footer = () => {
  const { info } = useClientInfo();

  const style = useSpring({
    from: { marginBottom: -5, opacity: 0 },
    to: { marginBottom: 0, opacity: 1 },
  });

  return (
    <Container className="text-sm p-4 text-center space-y-1 overflow-hidden">
      <animated.div style={style} className="space-y-4">
        {useMemo(
          () => (
            <SupporterText />
          ),
          []
        )}

        {useMemo(
          () => (
            <div className="text-gray-darker">
              <VersionTooltip version={info.version} />
              <Split />
              <Anchor target="_blank" href="https://github.com/chiyadev/nhitomi">
                <LinkText>GitHub</LinkText>
              </Anchor>
              <Split />
              <Anchor target="_blank" href="https://discord.gg/JFNga7q">
                <LinkText>Discord</LinkText>
              </Anchor>
              <Split />
              <Anchor target="_blank" href="https://github.com/chiyadev/nhitomi/wiki/API">
                <LinkText>API</LinkText>
              </Anchor>
              <Split />
              <Anchor target="_blank" href="https://chiya.dev">
                <LinkText>chiya.dev</LinkText>
              </Anchor>
            </div>
          ),
          [info.version]
        )}
      </animated.div>
    </Container>
  );
};

const Split = () => <span className="mx-2">·</span>;

const VersionTooltip = ({ version }: { version: string }) => (
  <Tooltip className="inline-flex" overlayClassName="text-center" placement="top" overlay={version}>
    <Anchor target="_blank" href={`https://github.com/chiyadev/nhitomi/commit/${version}`}>
      <LinkText>b.{version.substring(0, 7)}</LinkText>
    </Anchor>
  </Tooltip>
);

const LinkText = ({ children }: { children?: ReactNode }) => {
  const [hover, setHover] = useState(false);

  const style = useSpring({
    color: hover ? getColor("white").rgb : getColor("gray", "darker").rgb,
  });

  return (
    <animated.span style={style} onMouseEnter={() => setHover(true)} onMouseLeave={() => setHover(false)}>
      {children}
    </animated.span>
  );
};

const SupporterText = () => {
  const { isSupporter } = useClientInfo();

  // until https://github.com/pmndrs/react-spring/issues/1160 gets resolved...
  const animationDisabled = useRef(false);
  animationDisabled.current = useConfig("animation")[0] === "none";

  const heartStyle = useSpring({
    to: async (next) => {
      while (!animationDisabled.current) {
        await next({ transform: "scale(1) rotate(0deg)" });
        await next({ transform: "scale(0.9) rotate(5deg)" });
      }
    },
  });

  if (!isSupporter) return null;

  return (
    <div>
      <SupportLink>
        <FormattedMessage id="components.footer.supporter" />{" "}
        <animated.div className="inline-flex" style={heartStyle}>
          <HeartFilled className="text-pink" />
        </animated.div>
      </SupportLink>
    </div>
  );
};
