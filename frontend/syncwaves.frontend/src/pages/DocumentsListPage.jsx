import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Cookie from "js-cookie";
import api from "../api";

export default function DocumentsListPage() {
    const [docs, setDocs] = useState([]);
    const [loading, setLoading] = useState(true);
    const navigate = useNavigate();
    const { t, i18n } = useTranslation();

    const currentLang = (i18n.language || "en").startsWith("ro") ? "ro" : "en";

    useEffect(() => {
        const token = Cookie.get("syncwaves_token");
        if (!token) return;

        const fetchDocs = async () => {
            try {
                const resp = await api.get(process.env.REACT_APP_DOC_URL + "/documents/mine", {
                    headers: {
                        Authorization: `Bearer ${token}`,
                        "Accept-Language": currentLang
                    },
                });
                if (resp.status === 204) {
                    toast.info(`${t("documentList.empty")} ${t("documentList.createnewone")}`);
                }
                setDocs(resp.data);
            } catch (err) {
                console.error(err);
                toast.error(t("documentList.failedload"));
            } finally {
                setLoading(false);
            }
        };

        fetchDocs();
    }, []);

    const createDocument = async (e) => {
        e.preventDefault();

        try {
            const token = Cookie.get("syncwaves_token");
            const resp = await api.post(
                process.env.REACT_APP_DOC_URL + "/documents",
                { title: t("editor.untitled"), content: "" },
                {
                    headers: {
                        Authorization: `Bearer ${token}`,
                        "Accept-Language": currentLang
                    },
                }
            );
            const newDoc = resp.data;
            console.log(newDoc)
            setDocs((prev) => [newDoc, ...prev]);
            navigate(`/documents/${newDoc.id}`);
            toast.success(t("documentList.success"));
        } catch (err) {
            console.error(err);
            toast.error(t("documentList.failedcreate"));
        }
    };

    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50">
                <p className="text-gray-500">{t("documentList.loading")}</p>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50 flex justify-center p-6">
            <div className="max-w-4xl w-full">
                <div className="bg-white rounded-2xl shadow-lg p-8">
                    <div className="flex items-center justify-between mb-6">
                        <h1 className="text-2xl font-bold text-gray-900">{t("documentList.your")}</h1>

                        <form onSubmit={createDocument} className="flex gap-2">
                            <button
                                type="submit"
                                className="px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium hover:bg-blue-700">
                                {t("documentList.create")}
                            </button>
                        </form>

                    </div>

                    {docs.length === 0 ? (
                        <p className="text-gray-500 text-sm">
                            {t("documentList.empty")}
                            {t("documentList.createnewone")}
                        </p>
                    ) : (
                        <ul className="divide-y divide-gray-100">
                            {docs.map((doc) => (
                                <li
                                    key={doc.id}
                                    className="py-3 cursor-pointer hover:bg-gray-50 rounded-lg px-2 -mx-2"
                                    onClick={() => navigate(`/documents/${doc.id}`)} >
                                    <div className="flex items-center justify-between">
                                        <div>
                                            <div className="font-medium text-gray-900">{doc.title || "(Untitled)"}</div>
                                            <div className="text-xs text-gray-500">
                                                {t("documentList.createdat")} {new Date(doc.createdAt).toLocaleString()} {doc.createdAt === doc.updatedAt ? " " : `${t("documentList.updatedat")} ${new Date(doc.updatedAt).toLocaleString()}`}
                                            </div>
                                        </div>
                                        <span className="text-xs text-gray-400">{t("documentList.open")}</span>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            </div>
        </div>
    );
}
