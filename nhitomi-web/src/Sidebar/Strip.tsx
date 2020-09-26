import React, { ReactNode, useLayoutEffect, useMemo, useRef, useState } from "react";
import { css, cx } from "emotion";
import {
  BookOutlined,
  CloudDownloadOutlined,
  CloudFilled,
  FolderOpenFilled,
  FolderOutlined,
  HeartFilled,
  HeartOutlined,
  InfoCircleFilled,
  InfoCircleOutlined,
  ReadFilled,
  SettingFilled,
  SettingOutlined,
} from "@ant-design/icons";
import { RoundIconButton } from "../Components/RoundIconButton";
import { Tooltip } from "../Components/Tooltip";
import { FormattedMessage } from "react-intl";
import { BookListingLink } from "../BookListing";
import { SettingsLink } from "../Settings";
import { Route, Switch } from "react-router-dom";
import { animated, useSpring } from "react-spring";
import { SelfCollectionListingLink } from "../CollectionListing";
import { useClientInfo } from "../ClientManager";
import { Disableable } from "../Components/Disableable";
import { AboutLink } from "../About";
import { SupportLink } from "../Support";
import { SidebarStripWidth } from "../LayoutManager";
import { DownloadsLink } from "../Downloads";
import { useDownloads } from "../DownloadManager";
import { getColor } from "../theme";

export const Strip = ({ children, additionalMenu }: { children?: ReactNode; additionalMenu?: ReactNode }) => {
  const { user } = useClientInfo();

  const style = useSpring({
    from: { opacity: 0, marginLeft: -5 },
    to: { opacity: 1, marginLeft: 0 },
  });

  return (
    <>
      <div
        className={css`
          padding-left: ${SidebarStripWidth}px;
        `}
      >
        {children}
      </div>

      <animated.div
        style={style}
        className={cx(
          "fixed top-0 left-0 bottom-0 z-10 text-white py-4 select-none",
          css`
            width: ${SidebarStripWidth}px;
          `
        )}
      >
        <Disableable disabled={!user} className="flex flex-col items-center">
          <Logo />
          <Buttons />

          {additionalMenu}
        </Disableable>
      </animated.div>
    </>
  );
};

const Logo = () =>
  useMemo(
    () => (
      <Tooltip
        overlay={
          <span>
            <FormattedMessage id="pages.home.title" />
          </span>
        }
        placement="right"
        className="mb-4"
      >
        <BookListingLink>
          <img
            alt="logo"
            src="/logo-80x80.png"
            className={cx(
              "pointer-events-none rendering-crisp",
              css`
                /* integer width to prevent blurry downscale */
                width: 40px;
                height: 40px;
              `
            )}
          />
        </BookListingLink>
      </Tooltip>
    ),
    []
  );

const Buttons = () =>
  useMemo(
    () => (
      <>
        <Tooltip overlay={<FormattedMessage id="pages.bookListing.title" />} placement="right">
          <BookListingLink>
            <RoundIconButton>
              <Switch>
                <Route path="/books">
                  <ReadFilled />
                </Route>
                <Route>
                  <BookOutlined />
                </Route>
              </Switch>
            </RoundIconButton>
          </BookListingLink>
        </Tooltip>

        <Tooltip overlay={<FormattedMessage id="pages.collectionListing.title" />} placement="right">
          <SelfCollectionListingLink>
            <RoundIconButton>
              <Switch>
                <Route path="/collections">
                  <FolderOpenFilled />
                </Route>
                <Route path="/users/:id/collections">
                  <FolderOpenFilled />
                </Route>
                <Route>
                  <FolderOutlined />
                </Route>
              </Switch>
            </RoundIconButton>
          </SelfCollectionListingLink>
        </Tooltip>

        <DownloadsButton />

        <Tooltip overlay={<FormattedMessage id="pages.settings.title" />} placement="right">
          <SettingsLink>
            <RoundIconButton>
              <Switch>
                <Route path="/settings">
                  <SettingFilled />
                </Route>
                <Route>
                  <SettingOutlined />
                </Route>
              </Switch>
            </RoundIconButton>
          </SettingsLink>
        </Tooltip>

        <Tooltip overlay={<FormattedMessage id="pages.support.title" />} placement="right">
          <SupportLink>
            <RoundIconButton>
              <Switch>
                <Route path="/support">
                  <HeartFilled className="text-pink" />
                </Route>
                <Route>
                  <HeartOutlined />
                </Route>
              </Switch>
            </RoundIconButton>
          </SupportLink>
        </Tooltip>

        <Tooltip overlay={<FormattedMessage id="pages.about.title" />} placement="right">
          <AboutLink>
            <RoundIconButton>
              <Switch>
                <Route path="/about">
                  <InfoCircleFilled />
                </Route>
                <Route>
                  <InfoCircleOutlined />
                </Route>
              </Switch>
            </RoundIconButton>
          </AboutLink>
        </Tooltip>
      </>
    ),
    []
  );

const DownloadsButton = () => {
  const { tasks } = useDownloads();
  const [changed, setChanged] = useState(false);

  const timeout = useRef<number>();

  useLayoutEffect(
    () => () => {
      setChanged(true);

      clearTimeout(timeout.current);
      timeout.current = window.setTimeout(() => setChanged(false), 200);
    },
    [tasks.length]
  );

  const style = useSpring({
    transform: changed ? "scale(1.1)" : "scale(1)",
    color: changed ? getColor("blue").rgb : getColor("white").rgb,
    immediate: changed,
  });

  return (
    <animated.div style={style}>
      <Tooltip overlay={<FormattedMessage id="pages.downloads.title" />} placement="right">
        <DownloadsLink>
          <RoundIconButton>
            <Switch>
              <Route path="/downloads">
                <CloudFilled />
              </Route>
              <Route>
                <CloudDownloadOutlined />
              </Route>
            </Switch>
          </RoundIconButton>
        </DownloadsLink>
      </Tooltip>
    </animated.div>
  );
};
