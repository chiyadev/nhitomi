import React, { memo, ReactNode, useState } from "react";
import VisibilitySensor from "react-visibility-sensor";
import { useWindowSize } from "../utils/window";

const SimpleLazy = ({ render }: { render: (visible: boolean) => ReactNode }) => {
  const [state, setState] = useState(false);
  const [width, height] = useWindowSize() || [0, 0];

  return (
    <VisibilitySensor
      offset={{
        left: -width,
        right: -width,
        top: -height,
        bottom: -height,
      }}
      onChange={(visible) => {
        visible && setState(true);
      }}
    >
      {render(state)}
    </VisibilitySensor>
  );
};

export default memo(SimpleLazy);
