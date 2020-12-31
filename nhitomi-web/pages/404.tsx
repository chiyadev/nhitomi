import React, { memo } from "react";
import ErrorPage from "../components/ErrorPage";
import ConfigProvider from "../components/ConfigProvider";

const NotFound = () => {
  return (
    <ConfigProvider cookies={{}}>
      <ErrorPage message="Page not found" />
    </ConfigProvider>
  );
};

export default memo(NotFound);
