import P1 from "../assets/Placeholders/1.jpg";
import P2 from "../assets/Placeholders/2.jpg";
import P3 from "../assets/Placeholders/3.jpg";
import P4 from "../assets/Placeholders/4.jpg";
import P5 from "../assets/Placeholders/5.jpg";
import P6 from "../assets/Placeholders/6.jpg";
import P7 from "../assets/Placeholders/7.jpg";
import P8 from "../assets/Placeholders/8.jpg";
import P9 from "../assets/Placeholders/9.jpg";

const images = [P1, P2, P3, P4, P5, P6, P7, P8, P9];

export function getSfwPlaceholder() {
  const image = images[Math.floor(Math.random() * images.length)];

  return fetch(image).then((r) => r.blob());
}
