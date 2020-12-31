import React, { ComponentProps } from "react";
import dynamic from "next/dynamic";

const Core = dynamic(() => import("./Core"), { ssr: false });
const PageDisplay = (props: ComponentProps<typeof Core>) => <Core {...(props as any)} />;

export default PageDisplay;
