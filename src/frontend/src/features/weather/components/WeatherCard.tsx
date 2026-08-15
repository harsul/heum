import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Divider from '@mui/material/Divider';
import Typography from '@mui/material/Typography';
import type { WeatherForecast } from '../types/weather';

interface WeatherCardProps {
  forecast: WeatherForecast;
  useCelsius: boolean;
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  });
}

export function WeatherCard({ forecast, useCelsius }: WeatherCardProps) {
  const temp = useCelsius ? forecast.temperatureC : forecast.temperatureF;
  const unitLabel = useCelsius ? 'Celsius' : 'Fahrenheit';
  const formattedDate = formatDate(forecast.date);

  return (
    <Card
      component="article"
      elevation={3}
      aria-label={`Weather for ${formattedDate}`}
      sx={{ height: '100%', display: 'flex', flexDirection: 'column', borderRadius: 3 }}
    >
      <CardContent
        sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', gap: 1 }}
      >
        <Typography
          variant="overline"
          component="h3"
          color="text.secondary"
          sx={{ lineHeight: 1.5 }}
        >
          <time dateTime={forecast.date}>{formattedDate}</time>
        </Typography>

        <Typography variant="body1" sx={{ fontWeight: 500 }}>
          {forecast.summary}
        </Typography>

        <Divider sx={{ mt: 'auto', mb: 1 }} />

        <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <Typography
            variant="h4"
            sx={{ fontWeight: 700 }}
            aria-label={`${temp} degrees ${unitLabel}`}
          >
            <Box
              component="span"
              sx={{
                background: 'linear-gradient(135deg, #7c92f5 0%, #8b5ecf 100%)',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                backgroundClip: 'text',
              }}
            >
              {temp}°
            </Box>
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {unitLabel}
          </Typography>
        </Box>
      </CardContent>
    </Card>
  );
}
