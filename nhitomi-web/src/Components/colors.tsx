import { BookTag } from "nhitomi-api";
import { ColorHue } from "../theme";

export const BookTagColors: { [key in BookTag]: ColorHue } = {
  artist: "orange",
  circle: "yellow",
  character: "pink",
  // copyright: 'red',
  parody: "green",
  series: "lime",
  // pool: 'cyan',
  convention: "grape",
  metadata: "violet",
  tag: "blue",
};
