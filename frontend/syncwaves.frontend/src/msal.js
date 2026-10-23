import { PublicClientApplication, LogLevel } from "@azure/msal-browser";

const REDIRECT_URI = process.env.REACT_APP_MS_REDIRECT_URI;

export const msalInstance = new PublicClientApplication({
  auth: {
    clientId: process.env.REACT_APP_MS_CLIENT_ID,
    authority: `https://login.microsoftonline.com/${process.env.REACT_APP_MS_TENANT_ID}`,
    redirectUri: REDIRECT_URI,
    navigateToLoginRequestUrl: false,
  },
  cache: {
    temporaryCacheLocation: "localStorage",
    cacheLocation: "localStorage",
    storeAuthStateInCookie: false,
  },
   system: {
    loggerOptions: {
      logLevel: LogLevel.Verbose,
      piiLoggingEnabled: false,
      loggerCallback(level, message) {
        console.log("[MSAL]", message);
      },
    },
  },
});

export const msLoginRequest = {
  scopes: ["openid", "profile", "email"],
};

export async function initMsal() {
  await msalInstance.initialize();
}
