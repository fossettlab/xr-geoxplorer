#!/usr/bin/env node

import { readRequest } from "./reader.mjs";

const chunks = [];
process.stdin.on("data", (chunk) => chunks.push(chunk));
process.stdin.on("end", () => {
  process.stdout.write(JSON.stringify(readRequest(Buffer.concat(chunks).toString("utf8"))) + "\n");
});
