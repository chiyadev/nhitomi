import { BookTag, LanguageType, ScraperType } from "nhitomi-api";
import nhentai from "../public/assets/icons/nhentai.jpg";
import Hitomi from "../public/assets/icons/hitomi.jpg";

export const QueryChunkSize = 50;
export const NonSupporterPageLimit = 50;

export const BookTags = [
  BookTag.Artist,
  BookTag.Circle,
  BookTag.Character,
  BookTag.Parody,
  BookTag.Series,
  BookTag.Convention,
  BookTag.Metadata,
  BookTag.Tag,
];

export const BookTagColors: { [key in BookTag]: string } = {
  artist: "orange",
  circle: "yellow",
  character: "pink",
  // copyright: 'red',
  parody: "green",
  series: "lime",
  // pool: 'cyan',
  convention: "purple",
  metadata: "violet",
  tag: "blue",
};

export const ScraperTypes = [ScraperType.Nhentai, ScraperType.Hitomi];

export const LanguageTypes = [
  LanguageType.JaJp,
  LanguageType.EnUs,
  LanguageType.ZhCn,
  LanguageType.KoKr,
  LanguageType.ItIt,
  LanguageType.EsEs,
  LanguageType.DeDe,
  LanguageType.FrFr,
  LanguageType.TrTr,
  LanguageType.NlNl,
  LanguageType.RuRu,
  LanguageType.IdId,
  LanguageType.ViVn,
];

export const ScraperIcons: Record<ScraperType, string> = {
  [ScraperType.Unknown]: "",
  [ScraperType.Nhentai]: nhentai,
  [ScraperType.Hitomi]: Hitomi,
};
