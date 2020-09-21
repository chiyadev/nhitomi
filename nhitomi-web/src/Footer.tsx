import React, { ReactNode, useMemo, useState } from "react";
import { Container } from "./Components/Container";
import { animated, useSpring } from "react-spring";
import { getColor } from "./theme";
import { useClientInfo } from "./ClientManager";
import { Tooltip } from "./Components/Tooltip";
import { FormattedDate, FormattedMessage, FormattedTime } from "react-intl";
import { GitCommit } from "nhitomi-api";
import { Anchor } from "./Components/Anchor";
import { HeartFilled } from "@ant-design/icons";
import { SupportLink } from "./Support";

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
              <VersionTooltip version={info.version}>
                <Anchor target="_blank" href={`https://github.com/chiyadev/nhitomi/commit/${info.version.hash}`}>
                  <LinkText>b.{info.version.shortHash}</LinkText>
                </Anchor>
              </VersionTooltip>
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

const VersionTooltip = ({ version, children }: { version: GitCommit; children?: ReactNode }) => (
  <Tooltip
    className="inline-flex"
    overlayClassName="text-center"
    placement="top"
    overlay={
      <>
        <div>{version.hash}</div>
        <div>
          <FormattedDate value={version.time} /> <FormattedTime value={version.time} />
        </div>
      </>
    }
  >
    {children}
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
  const { info } = useClientInfo();

  const heartStyle = useSpring({
    to: async (next) => {
      for (;;) {
        await next({ transform: "scale(1) rotate(0deg)" });
        await next({ transform: "scale(0.9) rotate(5deg)" });
      }
    },
  });

  if (!info.authenticated || !info.user.isSupporter) return null;

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
