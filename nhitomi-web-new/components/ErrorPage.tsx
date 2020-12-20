import React, { memo } from "react";

const ErrorPage = ({ message }: { message: string }) => {
  return <div>{message}</div>;
};

export default memo(ErrorPage);
