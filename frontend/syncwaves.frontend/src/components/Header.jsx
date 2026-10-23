import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import Cookies from "js-cookie";
import { isMobile } from "react-device-detect";
import { toast } from "react-toastify";
import api from "../api";

const AUTH_TOKEN_KEY = "syncwaves_token";

export default function Header() {
    const [profileImageUrl, setProfileImageUrl] = useState(null);
    const [emailInitials, setEmailInitials] = useState("");
    const { t, i18n } = useTranslation();
    const navigate = useNavigate();

    const isAuthenticated = !!Cookies.get(AUTH_TOKEN_KEY);

    const handleLanguageChange = (e) => {
        const lng = e.target.value;
        i18n.changeLanguage(lng);
    };

    const handleLogout = () => {
        Cookies.remove(AUTH_TOKEN_KEY);
        navigate("/login");
    };

    const currentLang = (i18n.language || "en").startsWith("ro") ? "ro" : "en";

    useEffect(() => {
        const fetchMe = async () => {
            if (!isAuthenticated) return;
            try {
                var token = Cookies.get(AUTH_TOKEN_KEY);
                const resp = await api.get(process.env.REACT_APP_AUTH_URL + "/api/auth/logged", {
                    headers: { Authorization: `Bearer ${token}` },
                });

                setProfileImageUrl(resp.data.profileImageUrl || null);
                const email = resp.data.email || "";
                const letter = email ? email[0].toUpperCase() : "?";
                setEmailInitials(letter);
            } catch (err) {
                if (!err.response) {
                    toast.error(t("common.serverunreachable",));
                }
                else if(err.response.status === 404)
                {
                    toast.error(err.response.data.detail);
                }
                console.error("Failed to fetch me:", err);
            }
        };

        fetchMe();
    }, [isAuthenticated]);

    const handleProfileClick = () => {
        navigate("/settings");
    };

    return (
        <header className="w-full border-b bg-white">
            <div className="max-w-6xl mx-auto flex items-center justify-between px-4 py-3">
                <div
                    className="flex items-center gap-2 cursor-pointer"
                    onClick={() => isAuthenticated ? navigate("/documents") : navigate("/")} >
                    <div className="w-8 h-8 rounded-lg bg-blue-600 text-white flex items-center justify-center text-sm font-bold">
                        SW
                    </div>
                    <span className="font-semibold text-gray-900">SyncWaves</span>
                </div>

                <div className="flex items-center gap-4">
                    <div className="flex items-center gap-2 text-xs">
                        <label className="text-gray-500" htmlFor="lang-select">
                            {(isMobile ? "" : t("common.language", "Language"))}
                        </label>
                        <select
                            id="lang-select"
                            value={currentLang}
                            onChange={handleLanguageChange}
                            className="border border-gray-300 rounded px-2 py-1 text-xs bg-white focus:outline-none focus:ring-1 focus:ring-blue-400" >
                            <option value="en">English</option>
                            <option value="ro">Română</option>
                        </select>
                    </div>

                    {isAuthenticated && (
                        <>
                            <button
                                type="button"
                                onClick={handleProfileClick}
                                className="flex items-center gap-2">
                                {profileImageUrl ? (
                                    <img
                                        src={profileImageUrl}
                                        alt="Profile"
                                        className="w-8 h-8 rounded-full object-cover border" />
                                ) : (
                                    <div className="w-8 h-8 rounded-full bg-gray-800 text-white flex items-center justify-center text-xs font-semibold">
                                        {emailInitials || "?"}
                                    </div>
                                )}
                            </button>
                            <button
                                type="button"
                                onClick={handleLogout}
                                className="px-4 py-1.5 rounded-lg bg-red-500 text-white text-xs font-medium hover:bg-red-600 transition" >
                                {t("common.logout", "Log out")}
                            </button>
                        </>
                    )}
                </div>
            </div>
        </header>
    );
}
