import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  root: "desktop",
  base: "/desktop/",
  plugins: [react()],
  build: {
    outDir: "../../TmuxMobile.Server/wwwroot/desktop",
    emptyOutDir: true,
    sourcemap: false
  },
  server: {
    host: "127.0.0.1",
    port: 5174,
    proxy: {
      "/api": "http://127.0.0.1:5179",
      "/health": "http://127.0.0.1:5179",
      "/ws": { target: "ws://127.0.0.1:5179", ws: true }
    }
  }
});
