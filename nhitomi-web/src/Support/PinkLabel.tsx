import React, { ReactNode } from "react";

export const PinkLabel = ({ children }: { children?: ReactNode }) => {
  return (
    <div className="text-lg text-center">
      <span className="shadow-lg rounded bg-pink text-white p-2">— {children} —</span>
    </div>
  );
};
