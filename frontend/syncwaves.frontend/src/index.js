import React from "react";
import { createRoot } from "react-dom/client";
import App from "./App.jsx"; 
import "./index.css";
import "./i18n";
import { msalInstance } from "./msal.js";
import api from "./api.js";
import Cookies from "js-cookie";

(async () => {
  await msalInstance.initialize();

  const existingToken = Cookies.get("syncwaves_token");
  if (existingToken) {
    api.defaults.headers.common["Authorization"] = `Bearer ${existingToken}`;
  }

  try {
    const result = await msalInstance.handleRedirectPromise();
    console.log("[Boot] handleRedirectPromise result:", result);

    if (result?.idToken) {
      const resp = await api.post(
        `${process.env.REACT_APP_AUTH_URL}/api/auth/login/microsoft`,
        { idToken: result.idToken },
        { withCredentials: true }
      );

      const accessToken = resp?.data?.accessToken;

      if (accessToken) {
        Cookies.set("syncwaves_token", accessToken, {
          sameSite: "lax",
          secure: true,
          compatiblepath: "/",
        });

        api.defaults.headers.common["Authorization"] = `Bearer ${accessToken}`;
      }

      if (window.location.hash) {
        window.history.replaceState(
          {},
          document.title,
          window.location.pathname + window.location.search
        );
      }
    }
  } catch (e) {
    console.error("[Boot] MSAL redirect handling failed:", e);
  }

  createRoot(document.getElementById("root")).render(<App />);
})();