import { useState } from 'react';
import { useAuth } from 'react-oidc-context';
import Button from '@mui/material/Button';
import aspireLogo from '/Aspire.png';
import '../App.css';
import { useWeatherForecast } from '../hooks/useWeatherForecast';

export function Dashboard() {
  const auth = useAuth();
  const [useCelsius, setUseCelsius] = useState(false);

  const { data: weatherData = [], isFetching, isError, error, refetch } = useWeatherForecast();

  const loading = isFetching;
  const fetchWeatherForecast = refetch;

  const formatDate = (dateString: string) =>
    new Date(dateString).toLocaleDateString(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
    });

  const displayName =
    auth.user?.profile.preferred_username ??
    auth.user?.profile.email ??
    'User';

  return (
    <div className="app-container">
      <header className="app-header">
        <div className="dashboard-top-bar">
          <a
            href="https://aspire.dev"
            target="_blank"
            rel="noopener noreferrer"
            aria-label="Visit Aspire website (opens in new tab)"
            className="logo-link"
          >
            <img src={aspireLogo} className="logo" alt="Aspire logo" />
          </a>
          <div className="user-info">
            <span className="username" aria-label={`Logged in as ${displayName}`}>
              {displayName}
            </span>
            <Button
              variant="outlined"
              size="small"
              onClick={() => auth.signoutRedirect()}
              aria-label={`Log out ${displayName}`}
              sx={{
                borderColor: 'var(--weather-card-border)',
                color: 'var(--text-secondary)',
                '&:hover': {
                  borderColor: 'var(--text-tertiary)',
                  backgroundColor: 'rgba(255,255,255,0.08)',
                  color: 'var(--text-primary)',
                },
              }}
            >
              Log Out
            </Button>
          </div>
        </div>
        <h1 className="app-title">Dashboard</h1>
        <p className="app-subtitle">Modern distributed application development</p>
      </header>

      <main className="main-content">
        <section className="weather-section" aria-labelledby="weather-heading">
          <div className="card">
            <div className="section-header">
              <h2 id="weather-heading" className="section-title">Weather Forecast</h2>
              <div className="header-actions">
                <fieldset className="toggle-switch" aria-label="Temperature unit selection">
                  <legend className="visually-hidden">Temperature unit</legend>
                  <button
                    className={`toggle-option ${!useCelsius ? 'active' : ''}`}
                    onClick={() => setUseCelsius(false)}
                    aria-pressed={!useCelsius}
                    type="button"
                  >
                    <span aria-hidden="true">°F</span>
                    <span className="visually-hidden">Fahrenheit</span>
                  </button>
                  <button
                    className={`toggle-option ${useCelsius ? 'active' : ''}`}
                    onClick={() => setUseCelsius(true)}
                    aria-pressed={useCelsius}
                    type="button"
                  >
                    <span aria-hidden="true">°C</span>
                    <span className="visually-hidden">Celsius</span>
                  </button>
                </fieldset>
                <button
                  className="refresh-button"
                  onClick={() => fetchWeatherForecast()}
                  disabled={loading}
                  aria-label={loading ? 'Loading weather forecast' : 'Refresh weather forecast'}
                  type="button"
                >
                  <svg
                    className={`refresh-icon ${loading ? 'spinning' : ''}`}
                    width="20"
                    height="20"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    aria-hidden="true"
                    focusable="false"
                  >
                    <path d="M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.2" />
                  </svg>
                  <span>{loading ? 'Loading...' : 'Refresh'}</span>
                </button>
              </div>
            </div>

            {isError && (
              <div className="error-message" role="alert" aria-live="polite">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                  <circle cx="12" cy="12" r="10" />
                  <line x1="12" y1="8" x2="12" y2="12" />
                  <line x1="12" y1="16" x2="12.01" y2="16" />
                </svg>
                <span>{error?.message ?? 'Failed to fetch weather data'}</span>
              </div>
            )}

            {loading && weatherData.length === 0 && (
              <div className="loading-skeleton" role="status" aria-live="polite" aria-label="Loading weather data">
                {[...Array(5)].map((_, i) => (
                  <div key={i} className="skeleton-row" aria-hidden="true" />
                ))}
                <span className="visually-hidden">Loading weather forecast data...</span>
              </div>
            )}

            {weatherData.length > 0 && (
              <div className="weather-grid">
                {weatherData.map((forecast, index) => (
                  <article key={index} className="weather-card" aria-label={`Weather for ${formatDate(forecast.date)}`}>
                    <h3 className="weather-date">
                      <time dateTime={forecast.date}>{formatDate(forecast.date)}</time>
                    </h3>
                    <p className="weather-summary">{forecast.summary}</p>
                    <div
                      className="weather-temps"
                      aria-label={`Temperature: ${useCelsius ? forecast.temperatureC : forecast.temperatureF} degrees ${useCelsius ? 'Celsius' : 'Fahrenheit'}`}
                    >
                      <div className="temp-group">
                        <span className="temp-value" aria-hidden="true">
                          {useCelsius ? forecast.temperatureC : forecast.temperatureF}°
                        </span>
                        <span className="temp-unit" aria-hidden="true">
                          {useCelsius ? 'Celsius' : 'Fahrenheit'}
                        </span>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </div>
        </section>
      </main>

      <footer className="app-footer">
        <nav aria-label="Footer navigation">
          <a href="https://aspire.dev" target="_blank" rel="noopener noreferrer">
            Learn more about Aspire<span className="visually-hidden"> (opens in new tab)</span>
          </a>
          <a
            href="https://github.com/dotnet/aspire"
            target="_blank"
            rel="noopener noreferrer"
            className="github-link"
            aria-label="View Aspire on GitHub (opens in new tab)"
          >
            <img src="/github.svg" alt="" width="24" height="24" aria-hidden="true" />
            <span className="visually-hidden">GitHub</span>
          </a>
        </nav>
      </footer>
    </div>
  );
}
