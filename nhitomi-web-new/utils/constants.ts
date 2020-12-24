import { BookTag, LanguageType, ScraperType } from "nhitomi-api";
import nhentai from "../assets/Icons/nhentai.jpg";
import Hitomi from "../assets/Icons/Hitomi.jpg";

export const QueryChunkSize = 50;

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
  LanguageType.JaJP,
  LanguageType.EnUS,
  LanguageType.ZhCN,
  LanguageType.KoKR,
  LanguageType.ItIT,
  LanguageType.EsES,
  LanguageType.DeDE,
  LanguageType.FrFR,
  LanguageType.TrTR,
  LanguageType.NlNL,
  LanguageType.RuRU,
  LanguageType.IdID,
  LanguageType.ViVN,
];

export const ScraperIcons: Record<ScraperType, string> = {
  [ScraperType.Unknown]: "",
  [ScraperType.Nhentai]: nhentai,
  [ScraperType.Hitomi]: Hitomi,
};
