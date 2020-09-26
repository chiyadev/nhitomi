import React, { useEffect, useMemo, useState } from "react";
import { animated, useSpring } from "react-spring";
import { cx } from "emotion";
import { Loading3QuartersOutlined, WarningTwoTone } from "@ant-design/icons";
import { Tooltip } from "./Tooltip";
import { FormattedMessage } from "react-intl";
import { probeImage } from "../imageUtils";
import { getColor } from "../theme";
import { useAsync } from "../hooks";
import { createCachedImageRef, getCachedImageRef, returnCachedImageRef } from "../imageCache";

function formatAspect(x: number) {
  return `${x * 100}%`;
}

export const CoverImage = ({
  cacheKey,
  onLoad,
  onLoaded,
  className,
  zoomIn,
  autoSize,
  defaultAspect,
  sizing = "cover",
}: {
  cacheKey?: string;
  onLoad: () => Promise<Blob> | Blob;
  onLoaded?: (image: { url: string; width: number; height: number }) => void;
  className?: string;
  zoomIn?: boolean;
  autoSize?: boolean;
  defaultAspect?: number;
  sizing?: "cover" | "contain";
}) => {
  const [loaded, setLoaded] = useState<{ url: string; width: number; height: number } | undefined>(() => {
    if (cacheKey) {
      const image = getCachedImageRef(cacheKey);
      if (image) onLoaded?.(image);

      return image;
    }
  });

  const [loading, setLoading] = useState(!loaded);
  const [loadingProlonged, setLoadingProlonged] = useState(false); // if load is prolonged, show loading indicator

  const { error } = useAsync(async () => {
    if (loaded) return;

    setLoading(true);
    const timer = window.setTimeout(() => setLoadingProlonged(true), 2000);

    try {
      const blob = await onLoad();
      const { width, height } = await probeImage(blob);

      let url: string;

      if (cacheKey) url = createCachedImageRef(cacheKey, { blob, width, height });
      else url = URL.createObjectURL(blob);

      const loaded = { url, width, height };

      onLoaded?.(loaded);
      setLoaded(loaded);
    } finally {
      setLoading(false);
      clearTimeout(timer);
    }
  }, []);

  // revocation can run async
  useEffect(
    () => () => {
      if (loaded) {
        if (cacheKey) returnCachedImageRef(cacheKey);
        else URL.revokeObjectURL(loaded.url);
      }
    },
    [cacheKey, loaded]
  );

  const [showImage, setShowImage] = useState(!!loaded);
  const imageStyle = useSpring({
    opacity: loaded ? 1 : 0,
    transform: loaded || !zoomIn ? "scale(1)" : "scale(0.9)",
    onChange: {
      opacity: (v) => setShowImage(v > 0),
    },
  });

  const [showLoading, setShowLoading] = useState(loading && loadingProlonged);
  const loadingStyle = useSpring({
    opacity: loadingProlonged && loading ? 1 : 0,
    onChange: {
      opacity: (v) => setShowLoading(v > 0),
    },
  });

  const [showError, setShowError] = useState(!!error);
  const errorStyle = useSpring({
    opacity: error ? 1 : 0,
    onChange: {
      opacity: (v) => setShowError(v),
    },
  });

  const imageNode = useMemo(
    () =>
      showImage && (
        <animated.div
          style={{
            ...imageStyle,
            backgroundImage: loaded ? `url(${loaded.url})` : undefined, // don't use emotion for perf
            backgroundSize: sizing,
            backgroundPosition: "center",
            backgroundRepeat: "no-repeat",
          }}
          className="absolute top-0 left-0 w-full h-full"
        />
      ),
    [showImage, imageStyle, loaded, sizing]
  );

  const loadingNode = useMemo(
    () =>
      showLoading && (
        <animated.div style={loadingStyle} className="absolute transform-center">
          <Loading3QuartersOutlined className="animate-spin" />
        </animated.div>
      ),
    [showLoading, loadingStyle]
  );

  const errorNode = useMemo(
    () =>
      showError && (
        <animated.div style={errorStyle} className="absolute transform-center">
          <Tooltip
            placement="bottom"
            overlay={
              <>
                <div>
                  <FormattedMessage id="components.coverImage.error" />
                </div>
                <div>
                  <code>{error?.message || <FormattedMessage id="components.coverImage.errorUnknown" />}</code>
                </div>
              </>
            }
          >
            <WarningTwoTone twoToneColor={getColor("red").hex} />
          </Tooltip>
        </animated.div>
      ),
    [showError, errorStyle, error]
  );

  const style = useMemo(
    () => ({
      paddingTop:
        loaded && autoSize
          ? formatAspect(loaded.height / loaded.width)
          : defaultAspect
          ? formatAspect(defaultAspect)
          : undefined,
    }),
    [loaded, autoSize, defaultAspect]
  );

  const cname = useMemo(() => cx("relative select-none", className), [className]);

  return useMemo(
    () => (
      <div style={style} className={cname}>
        {imageNode}
        {loadingNode}
        {errorNode}
      </div>
    ),
    [style, cname, imageNode, loadingNode, errorNode]
  );
};
