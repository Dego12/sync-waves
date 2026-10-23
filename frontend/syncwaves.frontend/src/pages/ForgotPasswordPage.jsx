import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import api from "../api";

export default function FogotPasswordPage() {
    const navigate = useNavigate();
    const [status, setStatus] = useState("loading");
    const [message, setMessage] = useState("");
    const [email, setEmail] = useState("");
    const [loading, setLoading] = useState(false);
    const { t, i18n } = useTranslation();

    const currentLang = (i18n.language || "en").startsWith("ro") ? "ro" : "en";

    async function onSubmit(e) {
        e.preventDefault();
        setLoading(true);
        try {
            const resp = await api.put(process.env.REACT_APP_AUTH_URL + "/api/auth/forgot-password",
                {
                    email
                },
                {
                    headers: {
                        "Accept-Language": currentLang
                    }
                }
            );
        } catch (err) {
            console.log(err);
            setStatus("error");
            if (!err.response) {
                setMessage(t("common.serverunreachable"));
            } else if (err.response.status === 400 || err.response.status === 404) {
                setMessage(err.response.data.detail);
            }
            else {
                setMessage(t("common.somethingwentwrong"));
            }
        }
        finally {
            setLoading(false);
            setStatus("success");
        }
    }

    return (
        <div className="min-h-screen bg-gray-50 flex h-96 items-center justify-center p-6">
            <div className="max-w-lg w-full">
                <div className="mx-auto bg-white rounded-2xl shadow-lg overflow-hidden">
                    <div className="p-10 sm:p-12">
                        <h2 className="text-3xl font-extrabold text-gray-900 mb-4">{t("forgotpassword.forgotpassword")}</h2>

                        {status !== "success" ?
                            <>
                                <p className="text-sm text-gray-500 mb-6">{t("forgotpassword.email")}</p>
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
                                        <button
                                            type="submit"
                                            disabled={loading}
                                            className="mt-4 w-full py-3 rounded-md bg-blue-600 text-white font-medium hover:bg-blue-700 disabled:opacity-60">
                                            {loading ? t("common.loading") : t("forgotpassword.send")}
                                        </button>
                                    </div>
                                </form>
                            </>
                            :
                            <div className="mt-6 text center text-sm text-gray-500">
                                {t("forgotpassword.sent")}
                            </div>
                        }

                        {status === "error" && (
                            <>
                                <h1 className="text-xl font-semibold mb-2">{t("forgotpassword.failed")}</h1>
                                <p className="text-sm text-red-600 mb-4">{message}</p>
                                <button
                                    onClick={() => navigate("/login")}
                                    className="px-4 py-2 rounded-lg bg-gray-800 text-white text-sm font-medium hover:bg-gray-900">
                                    {t("forgotpassword.back")}
                                </button>
                            </>
                        )}
                    </div>
                </div>
            </div>
        </div>
    )
}