import React, { useEffect, useRef, useState } from "react";
import VisibilitySensor from "react-visibility-sensor";
import { animated, useSpring } from "react-spring";
import { Loading3QuartersOutlined } from "@ant-design/icons";

type LoadCallback = () => Promise<void>;

export const Loader = ({ loadMore }: { loadMore?: LoadCallback }) => {
  const visible = useRef(false);
  const loading = useRef(false);

  const [spinner, setSpinner] = useState(false);
  const spinnerStyle = useSpring({
    opacity: loadMore && spinner ? 1 : 0,
  });

  const [continuation, setContinuation] = useState<{ resolve: (callback?: LoadCallback) => void }>(); // must wrap in object because setState can accept a function

  // useEffect because continuation must be called after layout
  useEffect(() => continuation?.resolve(loadMore), [continuation, loadMore]);

  const load = async () => {
    if (loading.current || !loadMore) return;

    loading.current = true;
    setSpinner(true);

    try {
      // keep loading while visible
      while (visible.current && loadMore) {
        // run callback, items will be updated by parent
        await loadMore();

        // receive new callback with closure of the updated items
        loadMore = await new Promise((resolve) => setContinuation({ resolve }));
      }
    } finally {
      loading.current = false;
      setSpinner(false);
    }
  };

  return (
    <VisibilitySensor
      delayedCall
      partialVisibility
      onChange={(v) => {
        (visible.current = v) && load();
      }}
      offset={{ top: -400, bottom: -400 }}
    >
      {loadMore ? (
        <div className="relative w-full h-20">
          <animated.div style={spinnerStyle} className="absolute transform-center">
            <Loading3QuartersOutlined className="animate-spin" />
          </animated.div>
        </div>
      ) : (
        <div />
      )}
    </VisibilitySensor>
  );
};
