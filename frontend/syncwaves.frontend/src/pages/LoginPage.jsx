import { useEffect, useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import Cookies from "js-cookie";
import api from "../api";
import { msalInstance, msLoginRequest } from "../msal";

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [msLoginBusy, setMsLoginBusy] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const { t, i18n } = useTranslation();

  useEffect(() => {
    const isAuthenticated = Cookies.get("syncwaves_token");
    if (isAuthenticated) {
      navigate("/documents");
    }
  }, []);

  const currentLang = (i18n.language || "en").startsWith("ro") ? "ro" : "en";

  async function onSubmit(e) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const from = location.state?.from?.pathname || "/documents";
      const resp = await api.post(process.env.REACT_APP_AUTH_URL + "/api/auth/login",
        {
          email,
          password,
          rememberMe,
        },
        {
          withCredentials: true
        },
        {
          headers: {
            "Accept-Language": currentLang,
          }
        }
      );

      const { accessToken } = resp.data;
      if (!accessToken) throw new Error(t("login.notoken"));

      Cookies.set("syncwaves_token", accessToken, {
        secure: true,
        sameSite: "lax"
      });

      api.defaults.headers.common["Authorization"] = `Bearer ${accessToken}`;
      navigate(from, { replace: true });
    } catch (err) {
      console.error(err);
      if (!err.response) {
        setError(t("common.serverunreachable",));
      } else if (err.response.status === 400 ||
        err.response.status === 401 ||
        err.response.status == 404) {
        setError(err.response.data.detail);
      }
      else {
        setError(t("login.failed"));
      }
    } finally {
      setLoading(false);
    }
  }

  const handleMicrosoftLogin = async () => {
    if (msLoginBusy) return;
    setMsLoginBusy(true);

    try {
      await msalInstance.initialize();

      await msalInstance.loginRedirect({
        ...msLoginRequest,
        prompt: "select_account",
      });
    } catch (err) {
      console.error("[LoginPage] Microsoft login redirect failed", err);
      setMsLoginBusy(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-6">
      <div className="max-w-lg w-full">
        <div className="mx-auto bg-white rounded-2xl shadow-lg overflow-hidden">
          <div className="p-10 sm:p-12">
            <h2 className="text-3xl font-extrabold text-gray-900 mb-4">{t("login.welcome")}</h2>
            <p className="text-sm text-gray-500 mb-6">{t("login.enterdetails")}</p>

            <form onSubmit={onSubmit} className="space-y-4">
              <div>
                <label className="sr-only">{t("common.email")}</label>
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder={t("common.email")}
                  className="block w-full px-4 py-3 rounded-lg border border-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-400" />
              </div>

              <div>
                <label className="sr-only">{t("common.password")}</label>
                <input
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder={t("common.password")}
                  className="block w-full px-4 py-3 rounded-lg border border-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-400" />
              </div>

              <div className="flex items-center justify-between text-sm">
                <label className="flex items-center gap-2 text-gray-600">
                  <input type="checkbox" className="h-4 w-4" checked={rememberMe} onChange={(e) => setRememberMe(e.target.checked)} />
                  <span>{t("login.remember")}</span>
                </label>
                <a className="text-blue-600 hover:text-blue-700" href="/forgot-password">{t("login.forgotpassword")}</a>
              </div>

              {error && (
                <div className="text-sm text-red-600 bg-red-50 p-2 rounded">{String(error)}</div>
              )}

              <div>
                <button
                  type="submit"
                  disabled={loading}
                  className="w-full py-3 rounded-md bg-blue-600 text-white font-medium hover:bg-blue-700 disabled:opacity-60"
                >
                  {loading ? t("common.loading") : t("login.signin")}
                </button>
              </div>
            </form>

            <div className="mt-4 flex items-center">
              <div className="flex-1 h-px bg-gray-200" />
              <span className="px-3 text-xs text-gray-400">OR</span>
              <div className="flex-1 h-px bg-gray-200" />
            </div>

            <button
              onClick={handleMicrosoftLogin}
              disabled={loading}
              className="mt-4 w-full py-2 rounded border border-gray-300 text-sm text-gray-700 hover:bg-gray-100 flex items-center justify-center gap-2">
              <span>{msLoginBusy ? t("common.loading") : t("login.microsoft")}</span>
            </button>

            <div className="mt-6 text-center text-sm text-gray-500">
              {t("login.noaccount")} <a href="/register" className="text-blue-600">{t("login.signup")}</a>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
