import axios from "axios";
import Cookies from "js-cookie";

const api = axios.create({
    baseURL: process.env.REACT_APP_API_BASE_URL,
    withCredentials: true,
});

api.interceptors.request.use((config) => {
    const token = Cookies.get("syncwaves_token");
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

let isRefreshing = false;
let refreshPromise = null;

api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const original = error.config;

        if (!error.response) {
            return Promise.reject(error);
        }

        const status = error.response.status;

        if (
            status === 401 &&
            !original._retry &&
            !original.url.includes("/api/auth/login") &&
            !original.url.includes("/api/auth/refresh")
        ) {
            original._retry = true;

            try {
                if (!isRefreshing) {
                    isRefreshing = true;
                    refreshPromise = axios.post(
                        `${process.env.REACT_APP_AUTH_URL}/api/auth/refresh`, {},
                        {
                            withCredentials: true
                        }
                    );
                }

                const refreshResponse = await refreshPromise;
                const newToken =
                    refreshResponse.data.accessToken || refreshResponse.data.token;

                if (!newToken) {
                    throw new Error("No access token in refresh response");
                }

                Cookies.set("syncwaves_token", newToken, {
                    sameSite: "lax",
                });

                isRefreshing = false;
                refreshPromise = null;

                original.headers.Authorization = `Bearer ${newToken}`;
                return api(original);
            } catch (refreshErr) {
                isRefreshing = false;
                refreshPromise = null;

                Cookies.remove("syncwaves_token");
                return Promise.reject(refreshErr);
            }
        }

        return Promise.reject(error);
    }
);

export default api;
