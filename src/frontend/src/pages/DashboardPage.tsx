import Box from '@mui/material/Box';
import Grid from '@mui/material/Grid';
import Typography from '@mui/material/Typography';
import PeopleAltIcon from '@mui/icons-material/PeopleAltOutlined';
import ShoppingCartIcon from '@mui/icons-material/ShoppingCartOutlined';
import PaidIcon from '@mui/icons-material/PaidOutlined';
import TrendingUpIcon from '@mui/icons-material/TrendingUpOutlined';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { AppWidgetSummary } from '../components/widgets/AppWidgetSummary';
import { AppTasks } from '../components/widgets/AppTasks';
import { AppOrderTimeline } from '../components/widgets/AppOrderTimeline';

const tasks = [
  { id: '1', name: 'Review pull requests' },
  { id: '2', name: 'Deploy latest release' },
  { id: '3', name: 'Update onboarding docs' },
  { id: '4', name: 'Follow up with customers' },
];

const timelineEvents = [
  { id: '1', title: 'New order placed (#1832)', time: 'a few seconds ago', color: 'primary' as const },
  { id: '2', title: 'Server maintenance completed', time: '10 minutes ago', color: 'success' as const },
  { id: '3', title: 'New user registered', time: '1 hour ago', color: 'info' as const },
  { id: '4', title: 'Payment failed for invoice #221', time: '3 hours ago', color: 'error' as const },
];

export function DashboardPage() {
  return (
    <DashboardLayout>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" gutterBottom sx={{ fontWeight: 700 }}>
          Hi, welcome back 👋
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Here&apos;s what&apos;s happening with your app today.
        </Typography>
      </Box>

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <AppWidgetSummary title="Total Users" total="2,431" icon={PeopleAltIcon} color="primary" />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <AppWidgetSummary title="Orders" total="912" icon={ShoppingCartIcon} color="secondary" />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <AppWidgetSummary title="Revenue" total="$48.2k" icon={PaidIcon} color="success" />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <AppWidgetSummary title="Growth" total="+18%" icon={TrendingUpIcon} color="warning" />
        </Grid>
      </Grid>

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, md: 6 }}>
          <AppTasks title="Tasks" list={tasks} />
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <AppOrderTimeline title="Recent Activity" list={timelineEvents} />
        </Grid>
      </Grid>
    </DashboardLayout>
  );
}
