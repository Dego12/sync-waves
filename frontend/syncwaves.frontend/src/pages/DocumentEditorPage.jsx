import { useEffect, useRef, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import * as signalR from "@microsoft/signalr";
import ReactQuill, { Quill } from "react-quill-new";
import "react-quill-new/dist/quill.snow.css";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Cookies from "js-cookie";
import QuillCursors from "quill-cursors";
import api from "../api";

Quill.register("modules/cursors", QuillCursors);

export default function DocumentEditorPage() {
    const { id: documentId } = useParams();
    const [title, setTitle] = useState("");
    const [content, setContent] = useState("");
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [conflict, setConflict] = useState();
    const [version, setVersion] = useState(null);
    const [participants, setParticipants] = useState([]);

    const navigate = useNavigate();
    const { t, i18n } = useTranslation();

    const connectionRef = useRef(null);
    const quillRef = useRef(null);
    const lastCursorSentAtRef = useRef(0);
    const canEditRef = useRef(false);
    const contentSendTimeoutRef = useRef(null);
    const titleSendTimeoutRef = useRef(null);

    const currentLang = (i18n.language || "en").startsWith("ro") ? "ro" : "en";

    const token = Cookies.get("syncwaves_token");
    const claims = token ? parseJwt(token) : null;

    const currentUserId =
        claims?.nameid ||
        claims?.sub ||
        claims?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] ||
        null;

    const me = participants.find((p) => p.userId === currentUserId);
    const isOwner = !!me?.isOwner;
    const canEdit = isOwner || !!me?.canEdit;

    useEffect(() => {
        canEditRef.current = canEdit;
    }, [canEdit]);

    const quillModules = {
        toolbar: [
            [{ header: [1, 2, false] }],
            ["bold", "italic", "underline"],
            [{ list: "ordered" }, { list: "bullet" }],
            ["link"],
        ],
        cursors: {
            transformOnTextChange: true,
        },
    };

    function parseJwt(token) {
        try {
            const base64Url = token.split(".")[1];
            const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
            const jsonPayload = decodeURIComponent(
                atob(base64)
                    .split("")
                    .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
                    .join("")
            );

            return JSON.parse(jsonPayload);
        } catch {
            return null;
        }
    }

    useEffect(() => {
        const loadDoc = async () => {
            try {
                const token = Cookies.get("syncwaves_token");
                const resp = await api.get(
                    process.env.REACT_APP_DOC_URL + `/documents/${documentId}`,
                    {
                        headers: {
                            Authorization: `Bearer ${token}`,
                            "Accept-Language": currentLang,
                        },
                    }
                );
                setTitle(resp.data.title);
                setContent(resp.data.content || "");
                setVersion(resp.data.version);
            } catch (err) {
                if (!err.response) {
                    toast.error(t("editor.failed"));
                }
                else if (err.reponse.status === 404) {
                    toast.error(err.resposne.data.detail);
                }
                console.error(err);
            } finally {
                setLoading(false);
            }
        };

        loadDoc();
    }, [documentId, currentLang, t]);

    useEffect(() => {
        const token = Cookies.get("syncwaves_token");
        if (!token) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(process.env.REACT_APP_RTC_URL + "/hubs/document", {
                accessTokenFactory: () => token,
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connection.on("DocumentLoaded", (serverTitle, serverContent) => {
            setTitle((current) =>
                serverTitle && serverTitle.length > 0 ? serverTitle : current ?? "Untitled"
            );

            setContent((current) =>
                serverContent && serverContent.length > 0 ? serverContent : current ?? ""
            );
        });

        connection.on("TitleUpdated", (serverTitle) => {
            setTitle(serverTitle);
        });

        connection.on("ContentUpdated", (serverContent, fromUserId) => {
            setContent(serverContent);
        });

        connection.on("DocumentChanged", (msg) => {
            setContent(msg.content);
            setVersion(msg.version);
        });

        connection.on("PresenceUpdated", (users) => {
            const list = Array.isArray(users) ? users : [];

            const byUser = {};
            list.forEach((u) => {
                if (u && u.userId && !byUser[u.userId]) byUser[u.userId] = u;
            });
            const unique = Object.values(byUser);
            setParticipants(unique);

            const quill = quillRef.current?.getEditor();
            if (!quill) return;
            const cursors = quill.getModule("cursors");
            if (!cursors) return;

            const allowedIds = new Set(unique.map((u) => u.userId));

            Object.keys(cursors.cursors || {}).forEach((id) => {
                if (!allowedIds.has(id)) {
                    cursors.removeCursor(id);
                }
            });

            unique.forEach((u) => {
                if (!cursors.cursors || !cursors.cursors[u.userId]) {
                    cursors.createCursor(
                        u.userId,
                        u.displayName || "User",
                        u.color || "#3b82f6"
                    );
                }
            });
        });

        connection.on("CursorUpdated", (msg) => {
            const quill = quillRef.current?.getEditor();
            if (!quill) return;

            const cursors = quill.getModule("cursors");
            if (!cursors) return;

            const { userId, index, length, color, displayName } = msg;

            cursors.createCursor(
                userId,
                displayName || "User",
                color || "#3b82f6"
            );
            cursors.moveCursor(userId, { index, length });
        });

        connection.onreconnected(() => {
            console.log("Reconnected, rejoining document group...");
            toast.success(t("editor.reconnected"));
            connection
                .invoke("JoinDocument", documentId)
                .catch((err) => console.error("JoinDocument failed:", err));
        });

        connection.onreconnecting((err) => {
            console.log("Reconnecting...", err);
            toast.info(t("editor.reconnecting"));
        });

        connection.onclose((err) => {
            console.log("Closed", err);
            setParticipants([]);
        });

        connectionRef.current = connection;

        let selectionHandler = null;

        connection
            .start()
            .then(() => connection.invoke("JoinDocument", documentId))
            .then(() => {
                const quill = quillRef.current?.getEditor();
                if (!quill) return;

                selectionHandler = (range, oldRange, source) => {
                    if (source !== "user" || !range || !documentId || !canEditRef.current) {
                        return;
                    }

                    const now = Date.now();
                    if (now - lastCursorSentAtRef.current < 80) return;
                    lastCursorSentAtRef.current = now;

                    const conn = connectionRef.current;
                    if (!conn) return;

                    conn
                        .invoke("UpdateCursor", documentId, range.index, range.length)
                        .catch((err) =>
                            console.error("UpdateCursor error:", err)
                        );
                };

                quill.on("selection-change", selectionHandler);
            })
            .catch((err) => {
                console.error("SignalR connection error:", err);
                toast.error(t("editor.realtimefailed"));
            });

        return () => {
            const quill = quillRef.current?.getEditor();
            if (quill && selectionHandler) {
                quill.off("selection-change", selectionHandler);
            }

            if (contentSendTimeoutRef.current) {
                clearTimeout(contentSendTimeoutRef.current);
            }
            if (titleSendTimeoutRef.current) {
                clearTimeout(titleSendTimeoutRef.current);
            }

            connection.stop().catch((err) => console.error("Error stopping connection:", err));
        };

    }, [documentId, t, currentLang]);

    const handleToggleEdit = (targetUserId, newCanEdit) => {
        if (!connectionRef.current) return;

        connectionRef.current
            .invoke("SetEditPermission", documentId, targetUserId, newCanEdit)
            .catch((err) => console.error("SetEditPermission error:", err));
    };

    const scheduleSendContentUpdate = (newHtml) => {
        if (!canEditRef.current) return;
        const conn = connectionRef.current;
        if (!conn) return;

        if (contentSendTimeoutRef.current) clearTimeout(contentSendTimeoutRef.current);

        contentSendTimeoutRef.current = setTimeout(() => {
            conn.invoke("UpdateContent", documentId, newHtml).catch((err) =>
                console.error("UpdateContent error:", err)
            );
        }, 0);
    };

    const scheduleSendTitleUpdate = (newText) => {
        if (!canEditRef.current) return;
        const conn = connectionRef.current;
        if (!conn) return;

        if (titleSendTimeoutRef.current) clearTimeout(titleSendTimeoutRef.current);

        titleSendTimeoutRef.current = setTimeout(() => {
            conn.invoke("UpdateTitle", documentId, newText).catch((err) =>
                console.error("UpdateTitle error:", err)
            );
        }, 0);
    };

    const handleContentChange = (html, delta, source, editor) => {
        setContent(html);

        if (!canEditRef.current || source !== "user") {
            return;
        }

        scheduleSendContentUpdate(html);
    };

    const handleTitleChange = (text) => {
        setTitle(text);
        if (canEdit) {
            scheduleSendTitleUpdate(text);
        }
    };

    const saveDocument = async () => {
        if (!canEdit) {
            toast.error(t("editor.roleerror"));
            return;
        }
        setSaving(true);
        try {
            const token = Cookies.get("syncwaves_token");
            await api.put(
                process.env.REACT_APP_DOC_URL + `/documents`,
                {
                    documentId,
                    title,
                    content,
                    version
                },
                {
                    headers: {
                        Authorization: `Bearer ${token}`,
                        "Accept-Language": currentLang
                    },
                }
            );
            setVersion((v) => (v == null ? null : v + 1));
            toast.success(t("editor.successsave"));
        } catch (err) {
            console.error(err);
            if (err.response?.status === 409) {
                const current = err.response.data.current;

                setConflict({
                    serverContent: current.content,
                    serverVersion: current.version,
                    serverTitle: current.title,
                });
                toast.error(t("editor.conflictmessage"));
            }
            else if (err.response?.status === 404) {
                toast.error(err.response.data.detail);
            }
            else {
                toast.error(t("editor.savefailed"));
            }
        } finally {
            setSaving(false);
        }
    };

    const exportToPdf = async () => {
        try {
            const token = Cookies.get("syncwaves_token");
            const resp = await api.post(
                process.env.REACT_APP_DOC_URL + `/documents/export/pdf`,
                {
                    title,
                    content
                },
                {
                    responseType: "blob",
                    headers: {
                        Authorization: `Bearer ${token}`,
                        "Accept-Language": currentLang
                    }
                }
            );

            const blob = new Blob([resp.data], { type: "application/pdf" });
            const url = window.URL.createObjectURL(blob);

            const link = document.createElement("a");
            link.href = url;
            const safeTitle = (title || "document").replace(/[\/\\:*?"<>|]/g, "_");
            link.download = `${safeTitle}.pdf`;
            document.body.appendChild(link);
            link.click();
            link.remove();

            window.URL.revokeObjectURL(url);
        } catch (err) {
            console.error("Export PDF failed", err);
            toast.error(t("editor.exportFailed"));
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
            <div className="max-w-4xl w-full">
                <div className="bg-white rounded-2xl shadow-lg p-6 sm:p-8">
                    {participants.length > 0 && (
                        <div className="flex flex-wrap items-center gap-2 mb-3 text-xs">
                            <span className="text-gray-500 mr-1">
                                {t ? t("editor.activeusers") : "Active users"}:
                            </span>

                            {participants.map((u) => {
                                const isMe = u.userId === currentUserId;

                                return (
                                    <div
                                        key={u.userId + "-" + u.displayName}
                                        className="flex items-center gap-1 px-2 py-1 rounded-full bg-gray-100" >
                                        <span
                                            className="w-2 h-2 rounded-full"
                                            style={{ backgroundColor: u.color || "#9ca3af" }} />

                                        <span className="text-xs text-gray-800">
                                            {u.displayName || u.DisplayName || "User"}
                                            {isMe && t("editor.you")}
                                        </span>

                                        {u.isOwner && (
                                            <span className="text-[10px] uppercase px-1 rounded bg-amber-100 text-amber-800">
                                                {t("editor.owner")}
                                            </span>
                                        )}

                                        {u.canEdit && !u.isOwner && (
                                            <span className="text-[10px] uppercase px-1 rounded bg-green-100 text-green-800">
                                                {t("editor.editor")}
                                            </span>
                                        )}

                                        {!u.canEdit && !u.isOwner && (
                                            <span className="text-[10px] uppercase px-1 rounded bg-gray-200 text-gray-700">
                                                {t("editor.viewer")}
                                            </span>
                                        )}

                                        {isOwner && !isMe && (
                                            <button
                                                onClick={() => handleToggleEdit(u.userId, !u.canEdit)}
                                                className="ml-1 text-[10px] underline text-blue-600 hover:text-blue-800">
                                                {u.canEdit ? t("editor.deny") : t("editor.allow")}
                                            </button>
                                        )}
                                    </div>
                                );
                            })}
                        </div>
                    )}
                    <div className="flex items-center justify-between mb-4">
                        <button
                            onClick={() => navigate("/documents")}
                            className="text-sm text-gray-500 hover:text-gray-700">
                            {t("editor.back")}
                        </button>
                        <div>
                            {canEdit && (
                                <button
                                    onClick={saveDocument}
                                    disabled={saving}
                                    className="px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-60">
                                    {saving ? t("common.loading") : t("editor.save")}
                                </button>
                            )}
                            <button
                                onClick={exportToPdf}
                                className="px-3 py-2 ml-2 rounded-lg border border-gray-300 text-sm text-gray-700 hover:bg-gray-100">
                                {t("editor.exportpdf")}
                            </button>
                        </div>
                    </div>

                    <div className="mb-4">
                        <input
                            type="text"
                            value={title}
                            onChange={(e) => handleTitleChange(e.target.value)}
                            readOnly={!canEdit}
                            disabled={!canEdit}
                            className="w-full text-xl font-semibold bg-transparent focus:outline-none"
                        />
                    </div>

                    <ReactQuill
                        ref={quillRef}
                        value={content}
                        onChange={handleContentChange}
                        readOnly={!canEdit}
                        theme={"snow"}
                        modules={quillModules}
                        className="h-svh flex flex-col"
                    />
                </div>
            </div>
        </div>
    );
}
