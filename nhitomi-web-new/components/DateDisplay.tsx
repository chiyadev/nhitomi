import React, { ComponentProps, memo } from "react";
import { chakra, Tooltip } from "@chakra-ui/react";
import { FormattedDate, FormattedTime } from "react-intl";

const DateDisplay = ({ date, ...props }: { date: Date } & ComponentProps<typeof chakra.span>) => {
  return (
    <Tooltip
      maxW={undefined}
      label={
        <span>
          <span>{date.toDateString()}</span> <chakra.span color="blue.500">{date.toTimeString()}</chakra.span>
        </span>
      }
    >
      <chakra.span cursor="default" {...props}>
        <FormattedDate value={date} /> <FormattedTime value={date} />
      </chakra.span>
    </Tooltip>
  );
};

export default memo(DateDisplay);
