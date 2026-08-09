
import { Toaster } from "@/components/ui/toaster";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider } from "./contexts/AuthContext";
import { QueryProvider } from "./providers/QueryProvider";
import DataSourceSwitcher from "./components/DataSourceSwitcher";
import DataSourceDebug from "./components/DataSourceDebug";
import ProtectedRoute from "./components/ProtectedRoute";
import Index from "./pages/Index";
import Dashboard from "./pages/Dashboard";
import BookingPage from "./pages/BookingPage";
import Settings from "./pages/Settings";
import Register from "./pages/Register";
import Login from "./pages/Login";
import Search from "./pages/Search";
import Services from "./pages/Services";
import ServiceDetail from "./pages/ServiceDetail";
import ResetPassword from "./pages/ResetPassword";
import NotFound from "./pages/NotFound";

const App = () => (
  <QueryProvider>
    <TooltipProvider>
      <AuthProvider>
        <Toaster />
        <Sonner />
        <BrowserRouter>
          <Routes>
            <Route path="/" element={<Index />} />
            <Route path="/dashboard" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
            <Route path="/book/:providerId" element={<ProtectedRoute><BookingPage /></ProtectedRoute>} />
            <Route path="/settings" element={<ProtectedRoute><Settings /></ProtectedRoute>} />
            <Route path="/register" element={<Register />} />
            <Route path="/login" element={<Login />} />
            <Route path="/reset-password" element={<ResetPassword />} />
            <Route path="/search" element={<Search />} />
            <Route path="/service/:serviceId" element={<ServiceDetail />} />
            <Route path="/services" element={<ProtectedRoute><Services /></ProtectedRoute>} />
            {/* ADD ALL CUSTOM ROUTES ABOVE THE CATCH-ALL "*" ROUTE */}
            <Route path="*" element={<NotFound />} />
          </Routes>
        </BrowserRouter>
        <DataSourceSwitcher />
        <DataSourceDebug />
      </AuthProvider>
    </TooltipProvider>
  </QueryProvider>
);

export default App;
