import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import DesktopApp from "./DesktopApp";
import "@xterm/xterm/css/xterm.css";
import "./styles.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode><DesktopApp /></StrictMode>
);
