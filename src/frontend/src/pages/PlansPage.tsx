import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Chip from '@mui/material/Chip';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import { DashboardLayout } from '../layouts/dashboard/DashboardLayout';
import { NewPlanDialog } from '../features/plans/components/NewPlanDialog';
import { usePlans } from '../features/plans/hooks/usePlans';
import { useCreatePlan } from '../features/plans/hooks/usePlanMutations';
import { getApiErrorMessage } from '../utils/apiError';
import { formatDate } from '../utils/format';

export function PlansPage() {
  const navigate = useNavigate();
  const { data: plans = [], isLoading, isError } = usePlans();
  const createPlan = useCreatePlan();
  const [isNewOpen, setIsNewOpen] = useState(false);

  return (
    <DashboardLayout>
      <Card>
        <Stack
          direction="row"
          sx={{ alignItems: 'center', justifyContent: 'space-between', px: 3, pt: 3, pb: 2 }}
        >
          <Box>
            <Typography variant="h6">Plans</Typography>
            <Typography variant="body2" color="text.secondary">
              Billing tiers with entitlement rules.
            </Typography>
          </Box>
          <Button
            variant="contained"
            onClick={() => {
              createPlan.reset();
              setIsNewOpen(true);
            }}
          >
            New plan
          </Button>
        </Stack>

        {isError && (
          <Alert severity="error" sx={{ mx: 3, mb: 2 }}>
            Failed to load plans. Please try again.
          </Alert>
        )}

        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Entitlements</TableCell>
                <TableCell>Created</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading &&
                Array.from({ length: 3 }, (_, i) => (
                  <TableRow key={i}>
                    <TableCell><Skeleton variant="text" width={100} /></TableCell>
                    <TableCell><Skeleton variant="rounded" width={56} height={24} /></TableCell>
                    <TableCell><Skeleton variant="text" width={40} /></TableCell>
                    <TableCell><Skeleton variant="text" width={80} /></TableCell>
                  </TableRow>
                ))}

              {!isLoading &&
                plans.map((plan) => (
                  <TableRow
                    key={plan.id}
                    hover
                    sx={{ cursor: 'pointer' }}
                    onClick={() => navigate(`/admin/plans/${plan.id}`)}
                  >
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {plan.name}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={plan.isActive ? 'Active' : 'Inactive'}
                        color={plan.isActive ? 'success' : 'warning'}
                      />
                    </TableCell>
                    <TableCell>{plan.entitlements.length}</TableCell>
                    <TableCell>{formatDate(plan.createdAtUtc)}</TableCell>
                  </TableRow>
                ))}

              {!isLoading && plans.length === 0 && (
                <TableRow>
                  <TableCell colSpan={4} align="center" sx={{ py: 6 }}>
                    <Typography variant="subtitle1">No plans yet</Typography>
                    <Typography variant="body2" color="text.secondary">
                      Create your first billing plan to get started.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>

      <NewPlanDialog
        open={isNewOpen}
        saving={createPlan.isPending}
        errorMessage={getApiErrorMessage(createPlan.error, 'Failed to create plan.')}
        onClose={() => setIsNewOpen(false)}
        onCreate={(name) => {
          createPlan.mutate(name, { onSuccess: () => setIsNewOpen(false) });
        }}
      />
    </DashboardLayout>
  );
}
