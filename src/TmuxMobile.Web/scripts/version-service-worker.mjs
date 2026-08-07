import { createHash } from "node:crypto";
import { readFile, writeFile } from "node:fs/promises";

const outputRoot = new URL("../../TmuxMobile.Server/wwwroot/", import.meta.url);
const indexPath = new URL("index.html", outputRoot);
const serviceWorkerPath = new URL("service-worker.js", outputRoot);
const placeholder = "__TMUX_MOBILE_RELEASE__";

const index = await readFile(indexPath);
const release = createHash("sha256").update(index).digest("hex").slice(0, 16);
const serviceWorker = await readFile(serviceWorkerPath, "utf8");

if (!serviceWorker.includes(placeholder)) {
  throw new Error(`Expected ${placeholder} in generated service-worker.js.`);
}

await writeFile(serviceWorkerPath, serviceWorker.replaceAll(placeholder, release));
console.log(`Stamped service worker release ${release}.`);
