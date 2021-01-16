const fs = require("fs-extra");

fs.emptyDirSync("build/config");
fs.copySync("config", "build/config");

fs.emptyDirSync("build/Locales");
fs.copySync("src/Locales", "build/Locales");
