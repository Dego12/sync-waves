import { BrowserRouter, Routes, Route, Navigate, Outlet, useLocation } from "react-router-dom";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import DocumentsListPage from "./pages/DocumentsListPage";
import DocumentEditorPage from "./pages/DocumentEditorPage";
import VerifyEmailPage from "./pages/VerifyEmailPage";
import FogotPasswordPage from "./pages/ForgotPasswordPage";
import PasswordResetPage from "./pages/PasswordResetPage";
import Header from "./components/Header";
import { ToastContainer } from "react-toastify";
import Cookies from "js-cookie";
import UserSettingsPage from "./pages/UserSettingsPage";

function RequireAuth() {
  const location = useLocation();
  const token = Cookies.get("syncwaves_token");
  if (!token) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }
  return <Outlet/>;
}

export default function App() {
  return (
    <BrowserRouter>
      <Header />
      <main className="pt-4">
        <ToastContainer position="top-center"
                        autoClose={1000}
                        newestOnTop
                        closeOnClick
                        pauseOnHover
                        draggable
                        closeButton={false}/>
        <Routes>
          <Route path="/" element={<Navigate to="/login" replace />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route element={<RequireAuth />}>
            <Route path="/documents" element={<DocumentsListPage />} />
            <Route path="/documents/:id" element={<DocumentEditorPage />} />
            <Route path="/settings" element={<UserSettingsPage />} />
          </Route>
          <Route path="/verify-email" element={<VerifyEmailPage />} />
          <Route path="/forgot-password" element={<FogotPasswordPage />} />
          <Route path="/reset-password" element={<PasswordResetPage />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}