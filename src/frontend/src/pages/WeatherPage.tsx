import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { WeatherSection } from '../features/weather/components';

export function WeatherPage() {
  return (
    <DashboardLayout>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" gutterBottom sx={{ fontWeight: 700 }}>
          Weather
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Sample forecast data from the Aspire weather API.
        </Typography>
      </Box>

      <WeatherSection />
    </DashboardLayout>
  );
}
