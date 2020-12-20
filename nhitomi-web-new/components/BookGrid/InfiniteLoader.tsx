import React, { memo, useCallback, useEffect, useRef, useState } from "react";
import { Center, Icon, Spinner } from "@chakra-ui/react";
import { useInView } from "react-intersection-observer";

const InfiniteLoader = ({ hasMore }: { hasMore: () => Promise<boolean> }) => {
  const runningRef = useRef(false);
  const visibleRef = useRef(false);

  const [ref, visible] = useInView({ rootMargin: "100%" });
  const [end, setEnd] = useState(false);

  const beginLoad = useCallback(async () => {
    if (runningRef.current || end) return;
    runningRef.current = true;

    try {
      while (visibleRef.current) {
        try {
          if (!(await hasMore())) {
            setEnd(true);
            break;
          }
        } catch (e) {
          console.error(e);

          setEnd(true);
          break;
        }

        await new Promise((resolve) => setTimeout(resolve, 1000));
      }
    } finally {
      runningRef.current = false;
    }
  }, [end, hasMore]);

  useEffect(() => {
    visibleRef.current = visible;
    visible && beginLoad();
  }, [visible, beginLoad]);

  if (end) {
    return null;
  }

  return (
    <Center ref={ref} pt={12} pb={12}>
      <Icon as={Spinner} />
    </Center>
  );
};

export default memo(InfiniteLoader);
