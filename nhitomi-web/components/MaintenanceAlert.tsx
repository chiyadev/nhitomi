import React, { memo } from "react";
import { Alert, AlertIcon } from "@chakra-ui/react";
import { useT } from "../locales";
import { useClientInfo } from "../utils/client";
import LayoutBody from "./LayoutBody";

const MaintenanceAlert = () => {
  const t = useT();
  const info = useClientInfo();

  if (!info?.maintenance) {
    return null;
  }

  return (
    <div>
      <LayoutBody>
        <Alert variant="left-accent" status="error">
          <AlertIcon />
          {t("MaintenanceAlert.text")}
        </Alert>
      </LayoutBody>
    </div>
  );
};

export default memo(MaintenanceAlert);
