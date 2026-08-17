import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { TenantsTable } from '../features/tenants/components';

export function TenantsPage() {
  return (
    <DashboardLayout>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" gutterBottom sx={{ fontWeight: 700 }}>
          Tenants
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Companies onboarded to your platform.
        </Typography>
      </Box>

      <TenantsTable />
    </DashboardLayout>
  );
}
