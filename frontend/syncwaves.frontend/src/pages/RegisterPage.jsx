import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import api from "../api";

export default function LoginPage() {
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();
    const { t, i18n } = useTranslation();

    const currentLang = (i18n.language || "en").startsWith("ro") ? "ro" : "en";


    async function onSubmit(e) {
        e.preventDefault();
        setLoading(true);
        try {
            if (password !== confirmPassword) {
                throw new Error(t("register.passwordsdontmatch"));
            }
            console.log(process.env.REACT_APP_AUTH_URL);
            await api.post(process.env.REACT_APP_AUTH_URL + "/api/auth/register",
                {
                    name,
                    email,
                    password
                },
                {
                    headers: {
                        "Accept-Language": currentLang
                    }

                },
            )
            navigate("/login");
            toast.success(t("register.success"))
        }

        catch (err) {
            console.error(err);

            if (!err.response) {
                setError(err.message);
            } else if (err.response.status === 400 || 
                err.response.status == 409) {
                setError(err.response.data.detail);
            } else {
                setError(t("common.somethingwentwrong"));
            }
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="min-h-screen bg-gray-50 flex h-96 items-center justify-center p-6">
            <div className="max-w-lg w-full">
                <div className="mx-auto bg-white rounded-2xl shadow-lg overflow-hidden">
                    <div className="p-10 sm:p-12">
                        <h2 className="text-3xl font-extrabold text-gray-900 mb-4">{t("register.greetings")}</h2>
                        <p className="text-sm text-gray-500 mb-6">{t("register.details")}</p>

                        <form onSubmit={onSubmit} className="space-y-4">
                            <div>
                                <label className="sr-only">{t("common.name")}</label>
                                <input
                                    type="name"
                                    required
                                    value={name}
                                    onChange={(e) => setName(e.target.value)}
                                    placeholder={t("common.name")}
                                    className="block w-full px-4 py-3 rounded-lg border border-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-400"
                                />
                            </div>

                            <div>
                                <label className="sr-only">{t("common.email")}</label>
                                <input
                                    type="email"
                                    required
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    placeholder={t("common.email")}
                                    className="block w-full px-4 py-3 rounded-lg border border-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-400"
                                />
                            </div>

                            <div>
                                <label className="sr-only">{t("common.password")}</label>
                                <input
                                    type="password"
                                    required
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    placeholder={t("common.password")}
                                    className="block w-full px-4 py-3 rounded-lg border border-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-400"
                                />
                            </div>

                            <div>
                                <label className="sr-only">{t("common.confirmpassword")}</label>
                                <input
                                    type="password"
                                    required
                                    value={confirmPassword}
                                    onChange={(e) => setConfirmPassword(e.target.value)}
                                    placeholder={t("common.confirmpassword")}
                                    className="block w-full px-4 py-3 rounded-lg border border-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-400"
                                />
                            </div>

                            {error && (
                                <div className="text-sm text-red-600 bg-red-50 p-2 rounded">{String(error)}</div>
                            )}

                            <div>
                                <button
                                    type="submit"
                                    disabled={loading}
                                    className="mt-4 w-full py-3 rounded-md bg-blue-600 text-white font-medium hover:bg-blue-700 disabled:opacity-60">
                                    {loading ? t("common.loading") : t("register.signup")}
                                </button>
                            </div>
                        </form>

                        <div className="mt-6 text-center text-sm text-gray-500">
                            {t("register.alreadyaccount")} <a href="/login" className="text-blue-600">{t("register.login")}</a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
