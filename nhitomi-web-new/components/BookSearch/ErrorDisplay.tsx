import React, { memo } from "react";

const ErrorDisplay = ({ error }: { error: Error }) => {
  return <div>{error.message}</div>;
};

export default memo(ErrorDisplay);
