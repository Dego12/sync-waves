import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { isMobile, isWearable } from "react-device-detect";
import Cookie from "js-cookie";
import api from "../api";

const AUTH_TOKEN_KEY = "syncwaves_token";

export default function UserSettingsPage() {
    const { t, i18n } = useTranslation();
    const navigate = useNavigate();

    const [email, setEmail] = useState("");
    const [profileImageUrl, setProfileImageUrl] = useState(null);
    const [selectedFile, setSelectedFile] = useState(null);
    const [previewUrl, setPreviewUrl] = useState(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");

    const currentLang = (i18n.language || "en").startsWith("ro") ? "ro" : "en";
    const token = Cookie.get(AUTH_TOKEN_KEY);

    useEffect(() => {
        const loadMe = async () => {
            try {
                const resp = await api.get(process.env.REACT_APP_AUTH_URL + "/api/auth/logged", {
                    headers: { Authorization: `Bearer ${token}` },
                });

                setEmail(resp.data.email);
                setProfileImageUrl(resp.data.profileImageUrl || null);
            } catch (err) {
                if (err.response.status === 404) {
                    setError(err.response.data.detail);
                }
                else {
                    setError(t("settings.loadfailed"));
                }
                console.error(err);
            } finally {
                setLoading(false);
            }
        };

        loadMe();
    }, [navigate, t]);

    const handleFileChange = (e) => {
        const file = e.target.files?.[0];
        if (!file) return;

        setSelectedFile(file);
        setError("");

        const url = URL.createObjectURL(file);
        setPreviewUrl(url);
    };

    const handleSave = async () => {
        if (!selectedFile) return;

        setSaving(true);
        setError("");

        try {
            const formData = new FormData();
            formData.append("file", selectedFile);

            const resp = await api.put(
                process.env.REACT_APP_AUTH_URL + "/api/auth/profile-image",
                formData,
                {
                    headers: {
                        Authorization: `Bearer ${token}`,
                        "Content-Type": "multipart/form-data",
                        "Accept-Language": currentLang,
                    },
                }
            );

            setProfileImageUrl(resp.data.profileImageUrl || null);
            setSelectedFile(null);
            setPreviewUrl(null);
        } catch (err) {
            console.error(err);
            if (err.response?.status === 409 || err.response.status == 404) {
                setError(err.response.data.detail);
            } else {
                setError(t("settings.savefailed"));
            }
            console.log(err)
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50">
                <p className="text-gray-500">{t("common.loading")}</p>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50 flex justify-center p-6">
            <div className="max-w-lg w-full bg-white rounded-2xl shadow-lg p-6 sm:p-8">
                <h1 className="text-xl font-semibold mb-4">
                    {t("settings.title")}
                </h1>

                {error && <p className="text-sm text-red-600 mb-3">{error}</p>}

                <div className="mb-4">
                    <label className="block text-sm text-gray-600 mb-1">
                        {t("common.email")}
                    </label>
                    <input
                        type="text"
                        value={email}
                        disabled
                        className="w-full px-3 py-2 border rounded-lg text-sm bg-gray-100 text-gray-600" />
                </div>

                <div className="mb-4">
                    <label className="block text-sm text-gray-600 mb-2">
                        {t("settings.profileimage")}
                    </label>

                    <div className="flex items-center">
                        <div className="w-20 h-20 rounded-full bg-gray-200 flex items-center justify-center overflow-hidden border mr-4">
                            {previewUrl ? (
                                <img
                                    src={previewUrl}
                                    alt="Profile"
                                    className="w-20 h-20 rounded-full object-cover border" />
                            ) : profileImageUrl ? (
                                <img
                                    src={profileImageUrl}
                                    alt="Profile"
                                    className="w-20 h-20 rounded-full object-cover border" />
                            ) : (
                                <span className="text-xs text-gray-500">
                                    {t("settings.noimage")}
                                </span>
                            )}
                        </div>

                        <div className={`flex flex-col ${isMobile ? "ml-auto items-end" : ""}`}>
                            <input
                                type="file"
                                accept="image/png,image/jpeg,image/jpg,image/webp"
                                onChange={handleFileChange}
                                className={`text-xs ${isMobile ? "w-40" : ""}`} />
                            <p className="text-[11px] text-gray-400">
                                {t("settings.imagehint")}
                            </p>
                        </div>
                    </div>
                </div>

                <div className="flex justify-end gap-3 mt-4">
                    <button
                        onClick={() => navigate("/documents")}
                        className="px-4 py-2 text-sm rounded-lg border border-gray-300 text-gray-700 hover:bg-gray-100" >
                        {t("settings.back", "Back")}
                    </button>
                    <button
                        onClick={handleSave}
                        disabled={!selectedFile || saving}
                        className="px-4 py-2 text-sm rounded-lg bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-60" >
                        {saving
                            ? t("common.loading", "Loading...")
                            : t("settings.save", "Save")}
                    </button>
                </div>
            </div>
        </div>
    );
}
