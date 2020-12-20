import React, { memo, ReactNode, useEffect, useState } from "react";
import { useReaderScroll } from "./scroll";
import { useLastValue } from "../../utils/hooks";
import { chakra } from "@chakra-ui/react";

const CursorManager = ({ children }: { children?: ReactNode }) => {
  const [current] = useReaderScroll();
  const last = useLastValue(current);

  const [visible, setVisible] = useState(true);

  useEffect(() => {
    if (current.currentPage !== last.currentPage) {
      setVisible(false);
    }
  }, [current, last]);

  return (
    <chakra.div onMouseMove={() => setVisible(true)} cursor={visible ? undefined : "none"}>
      {children}
    </chakra.div>
  );
};

export default memo(CursorManager);
