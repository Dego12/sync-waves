import { useEffect, useState } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import api from "../api";

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState("loading");
  const [message, setMessage] = useState("");

  const { t, i18n } = useTranslation();

  const currentLang = (i18n.language || "en").startsWith("ro") ? "ro" : "en";

  useEffect(() => {
    const userId = searchParams.get("userId");
    const token = searchParams.get("token");

    console.log(userId);
    console.log(token);

    if (!userId || !token) {
      setStatus("error");
      setMessage(t("verify.invalidlink"));
      return;
    }

    const verify = async () => {
      try {
        await api.put(process.env.REACT_APP_AUTH_URL + "/api/auth/confirm-email",
          {
            userId,
            token,
          },
          {
            headers:
            {
              "Accept-Language": currentLang
            }
          }
        );
        setStatus("success");
        setMessage(t("verify.confirmed"));
      } catch (err) {
        console.error(err);
        setStatus("error");
        if (!err.response) {
          setMessage(t("common.serverunreachable",));
        } else if (err.response.status === 404 || err.response.status === 401) {
          setMessage(err.response.data.detail);
        }
        else {
          setMessage(err.response);
        }
      }
    };

    verify();
  }, [searchParams]);

  const goToLogin = () => navigate("/login");

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="bg-white shadow-lg rounded-2xl p-8 max-w-md w-full text-center">
        {status === "loading" && (
          <>
            <h1 className="text-xl font-semibold mb-2">{t("common.loading")}</h1>
            <p className="text-sm text-gray-500">
              {t("verify.wait")}
            </p>
          </>
        )}

        {status === "success" && (
          <>
            <h1 className="text-xl font-semibold mb-2">{t("verify.verified")}</h1>
            <p className="text-sm text-gray-600 mb-4">{message}</p>
            <button
              onClick={goToLogin}
              className="px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium hover:bg-blue-700">
              {t("verify.back")}
            </button>
          </>
        )}

        {status === "error" && (
          <>
            <h1 className="text-xl font-semibold mb-2">Verification failed</h1>
            <p className="text-sm text-red-600 mb-4">{message}</p>
            <button
              onClick={goToLogin}
              className="px-4 py-2 rounded-lg bg-gray-800 text-white text-sm font-medium hover:bg-gray-900">
              {t("verify.back")}
            </button>
          </>
        )}
      </div>
    </div>
  );
}