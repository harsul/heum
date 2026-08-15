import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import ToggleButton from '@mui/material/ToggleButton';
import ToggleButtonGroup from '@mui/material/ToggleButtonGroup';
import Typography from '@mui/material/Typography';
import RefreshIcon from '@mui/icons-material/Refresh';
import { useWeatherForecast } from '../hooks/useWeatherForecast';
import { WeatherCard } from './WeatherCard';

type TemperatureUnit = 'F' | 'C';

const CARD_GRID_SX = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
  gap: 2,
} as const;

export function WeatherSection() {
  const [unit, setUnit] = useState<TemperatureUnit>('F');
  const { data: forecasts = [], isFetching, isError, error, refetch } = useWeatherForecast();

  const handleUnitChange = (_: React.MouseEvent, value: TemperatureUnit | null) => {
    if (value) setUnit(value);
  };

  return (
    <Box component="section" aria-labelledby="weather-heading">
      <Stack
        direction="row"
        spacing={2}
        sx={{
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          mb: 3,
        }}
      >
        <Typography id="weather-heading" variant="h5" sx={{ fontWeight: 600 }}>
          Weather Forecast
        </Typography>

        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
          <ToggleButtonGroup
            value={unit}
            exclusive
            onChange={handleUnitChange}
            size="small"
            aria-label="Temperature unit"
          >
            <ToggleButton value="F" aria-label="Fahrenheit">°F</ToggleButton>
            <ToggleButton value="C" aria-label="Celsius">°C</ToggleButton>
          </ToggleButtonGroup>

          <Button
            variant="contained"
            size="medium"
            startIcon={
              <RefreshIcon
                sx={{
                  '@keyframes spin': {
                    from: { transform: 'rotate(0deg)' },
                    to: { transform: 'rotate(360deg)' },
                  },
                  animation: isFetching ? 'spin 1s linear infinite' : 'none',
                }}
              />
            }
            onClick={() => refetch()}
            disabled={isFetching}
            aria-label={isFetching ? 'Loading weather forecast' : 'Refresh weather forecast'}
          >
            {isFetching ? 'Loading…' : 'Refresh'}
          </Button>
        </Stack>
      </Stack>

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error?.message ?? 'Failed to fetch weather data'}
        </Alert>
      )}

      {isFetching && forecasts.length === 0 && (
        <Box role="status" aria-label="Loading weather data" sx={CARD_GRID_SX}>
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} variant="rounded" height={180} />
          ))}
        </Box>
      )}

      {forecasts.length > 0 && (
        <Box sx={CARD_GRID_SX}>
          {forecasts.map((forecast, index) => (
            <WeatherCard key={index} forecast={forecast} useCelsius={unit === 'C'} />
          ))}
        </Box>
      )}
    </Box>
  );
}
