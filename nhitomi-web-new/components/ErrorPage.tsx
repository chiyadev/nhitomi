import React, { memo } from "react";

const ErrorPage = ({ error }: { error: Error }) => {
  return <div>{error.message}</div>;
};

export default memo(ErrorPage);
