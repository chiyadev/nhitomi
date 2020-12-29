import React, { memo, useState } from "react";
import { useT } from "../../../locales";
import SectionItem from "../SectionItem";
import { Button, ButtonGroup, Icon } from "@chakra-ui/react";
import { FaRegCircle, FaTimes } from "react-icons/fa";
import { useErrorToast } from "../../../utils/hooks";
import BlockingSpinner from "../../BlockingSpinner";
import { createApiClient } from "../../../utils/client";

const Maintenance = () => {
  const t = useT();
  const error = useErrorToast();
  const [load, setLoad] = useState(false);

  const createButton = (value: boolean) => {
    return (
      <Button
        leftIcon={<Icon as={value ? FaRegCircle : FaTimes} />}
        onClick={async () => {
          setLoad(true);
          try {
            const client = createApiClient();

            await client.internal.setServerConfig({
              setConfigRequest: {
                key: "Server:BlockDatabaseWrites",
                value: value ? "true" : "false",
              },
            });
          } catch (e) {
            console.error(e);
            error(e);
          } finally {
            setLoad(false);
          }
        }}
      >
        {t("Settings.Debug.Maintenance.toggle", { value })}
      </Button>
    );
  };

  return (
    <SectionItem
      title={t("Settings.Debug.Maintenance.title")}
      description={t("Settings.Debug.Maintenance.description")}
    >
      <BlockingSpinner visible={load} />

      <ButtonGroup>
        {createButton(true)}
        {createButton(false)}
      </ButtonGroup>
    </SectionItem>
  );
};

export default memo(Maintenance);
