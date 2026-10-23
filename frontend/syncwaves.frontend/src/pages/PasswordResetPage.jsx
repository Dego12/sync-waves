import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import api from "../api";

export default function PasswordResetPage() {
    const [searchParams] = useSearchParams();
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState("");
    const navigate = useNavigate();
    const [status, setStatus] = useState("");
    const userId = searchParams.get("userId");
    const token = searchParams.get("token");
    const { t, i18n } = useTranslation();

    const currentLang = (i18n.language || "en").startsWith("ro") ? "ro" : "en";

    async function onSubmit(e) {
        console.log(userId);
        console.log(token);
        e.preventDefault();
        setStatus("loading");
        try {
            if (password !== confirmPassword) {
                throw new Error(t("register.passwordsdontmatch"));
            }

            await api.put(process.env.REACT_APP_AUTH_URL + "/api/auth/reset-password",
                {
                    userId,
                    password,
                    token
                },
                {
                    headers: {
                        "Accept-Language": currentLang
                    }
                }
            );
            setStatus("success");
        }
        catch (err) {
            console.log(err);
            setStatus("error");
            if (!err.response) {
                setError(err.response.data);
            } else if (err.response.status === 404 || err.response.status === 401) {
                setError(err.response.data.detail);
            }
            else {
                setError(err.message);
            }
        }
    }

    return (
        <div className="min-h-screen bg-gray-50 flex h-96 items-center justify-center p-6">
            <div className="max-w-lg w-full">
                <div className="mx-auto bg-white rounded-2xl shadow-lg overflow-hidden">
                    <div className="p-10 sm:p-12">
                        {status !== "success" ? (
                            <>
                                <h2 className="text-3xl font-extrabold text-gray-900 mb-4">{t("register.greetings")}</h2>
                                <p className="text-sm text-gray-500 mb-6">{t("reset.enter")}</p>

                                <form onSubmit={onSubmit} className="space-y-4">
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

                                    <div>
                                        <label className="sr-only">{t("common.confirmpassword")}</label>
                                        <input
                                            type="password"
                                            required
                                            value={confirmPassword}
                                            onChange={(e) => setConfirmPassword(e.target.value)}
                                            placeholder={t("common.confirmpassword")}
                                            className="block w-full px-4 py-3 rounded-lg border border-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-400" />
                                    </div>

                                    {error && (
                                        <div className="text-sm text-red-600 bg-red-50 p-2 rounded">{String(error)}</div>
                                    )}

                                    <div>
                                        <button
                                            type="submit"
                                            disabled={status === "loading"}
                                            className="mt-4 w-full py-3 rounded-md bg-blue-600 text-white font-medium hover:bg-blue-700 disabled:opacity-60">
                                            {status === "loading" ? t("common.loading") : t("reset.reset")}
                                        </button>
                                    </div>
                                </form>
                            </>
                        ) : (
                            <>
                                <h1 className="text-xl font-semibold mb-2">{t("reset.updated")}</h1>
                                <p className="text-sm text-gray-600 mb-4">{t("reset.updated2")}</p>
                                <button
                                    onClick={() => navigate("/login")}
                                    className="px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium hover:bg-blue-700">
                                    {t("verify.back")}
                                </button>
                            </>
                        )}

                    </div>
                </div>
            </div>
        </div>
    );
}