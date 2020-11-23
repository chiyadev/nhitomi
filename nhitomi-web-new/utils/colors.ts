import { BookTag } from "nhitomi-api";

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
