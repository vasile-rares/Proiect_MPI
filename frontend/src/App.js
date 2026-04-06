import { BrowserRouter, Routes, Route, Link, useNavigate, useLocation, Navigate } from "react-router-dom";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Game from "./pages/Game";
import Leaderboard from "./pages/Leaderboard";
import Profile from "./pages/Profile";
import History from "./pages/History";
import { isLoggedIn as checkAuth, logout, getUsername } from "./services/api";
import "./App.css";

function ProtectedRoute({ children }) {
  return checkAuth() ? children : <Navigate to="/" replace />;
}

function Header() {
  const navigate = useNavigate();
  const location = useLocation();
  const loggedIn = checkAuth();
  const username = getUsername();

  const handleLogout = () => {
    logout();
    navigate("/");
  };

  if (location.pathname === "/" || location.pathname === "/register") {
    return null;
  }

  return (
    <header className="header">
      <Link to="/game" className="logo">
        <span className="logo-icon">⌘</span>
        <span className="logo-text">key<span>less</span></span>
      </Link>
      <nav className="nav-links">
        <Link
          to="/game"
          className={`nav-link ${location.pathname === "/game" ? "active" : ""}`}
        >
          ⟳ test
        </Link>
        <Link
          to="/leaderboard"
          className={`nav-link ${location.pathname === "/leaderboard" ? "active" : ""}`}
        >
          ♛ leaderboard
        </Link>
        {loggedIn && (
          <Link
            to="/history"
            className={`nav-link ${location.pathname === "/history" ? "active" : ""}`}
          >
            📊 history
          </Link>
        )}
        {loggedIn && (
          <Link
            to="/profile"
            className={`nav-link ${location.pathname === "/profile" ? "active" : ""}`}
          >
            ⚙ {username || "profile"}
          </Link>
        )}
        {loggedIn && (
          <button className="nav-link" onClick={handleLogout}>
            ⏻ logout
          </button>
        )}
        {!loggedIn && (
          <Link
            to="/"
            className={`nav-link ${location.pathname === "/" ? "active" : ""}`}
          >
            ⏻ login
          </Link>
        )}
      </nav>
    </header>
  );
}

function App() {
  return (
    <BrowserRouter>
      <div className="app-container">
        <Header />
        <main className="main-content">
          <Routes>
            <Route path="/" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/game" element={<Game />} />
            <Route path="/leaderboard" element={<Leaderboard />} />
            <Route path="/profile" element={<ProtectedRoute><Profile /></ProtectedRoute>} />
            <Route path="/history" element={<ProtectedRoute><History /></ProtectedRoute>} />
          </Routes>
        </main>
        <footer className="footer">
          keyless — built with React & .NET
        </footer>
      </div>
    </BrowserRouter>
  );
}

export default App;