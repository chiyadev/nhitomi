import { ScraperType } from "nhitomi-api";
import nhentai from "../assets/icons/nhentai.jpg";
import hitomi from "../assets/icons/hitomi.jpg";

export const SourceIcons: Record<ScraperType, string> = {
  [ScraperType.Nhentai]: nhentai,
  [ScraperType.Hitomi]: hitomi,
  [ScraperType.Unknown]: "",
};
