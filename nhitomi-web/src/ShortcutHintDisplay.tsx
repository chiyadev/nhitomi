import { useShortcut } from "./shortcut";
import { usePrefetch } from "./Prefetch";
import { useSettingsPrefetch } from "./Settings";

export const ShortcutHintDisplay = () => {
  const [, navigate] = usePrefetch(useSettingsPrefetch, { focus: "shortcuts" });

  useShortcut("shortcutsKey", () => navigate("push"));

  return null;
};
