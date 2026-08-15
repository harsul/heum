import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { AppLayout } from '../components/AppLayout';
import { WeatherSection } from '../features/weather/components';

export function DashboardPage() {
  return (
    <AppLayout>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" gutterBottom sx={{ fontWeight: 700 }}>
          Dashboard
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Modern distributed application development
        </Typography>
      </Box>

      <WeatherSection />
    </AppLayout>
  );
}
