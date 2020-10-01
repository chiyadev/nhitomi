import React from "react";
import { Tooltip } from "../Components/Tooltip";
import { RoundIconButton } from "../Components/RoundIconButton";
import { useDownloads } from "../DownloadManager";
import { FormattedMessage } from "react-intl";
import { CloseOutlined, ReloadOutlined } from "@ant-design/icons";
import { trackEvent } from "../track";

export const Menu = () => (
  <>
    <RestartAllButton />
    <CancelAllButton />
  </>
);

const RestartAllButton = () => {
  const { tasks } = useDownloads();

  return (
    <Tooltip placement="bottom" overlay={<FormattedMessage id="pages.downloads.menu.restartAll" />}>
      <RoundIconButton
        onClick={() => {
          trackEvent("action", "downloadRestartAll");

          for (const task of tasks) {
            if (task.state.type !== "running") task.restart();
          }
        }}
      >
        <ReloadOutlined />
      </RoundIconButton>
    </Tooltip>
  );
};

const CancelAllButton = () => {
  const { tasks, remove } = useDownloads();

  return (
    <Tooltip placement="bottom" overlay={<FormattedMessage id="pages.downloads.menu.deleteAll" />}>
      <RoundIconButton
        onClick={() => {
          trackEvent("action", "downloadCancelAll");
          remove(...tasks.map((t) => t.id));
        }}
      >
        <CloseOutlined />
      </RoundIconButton>
    </Tooltip>
  );
};
