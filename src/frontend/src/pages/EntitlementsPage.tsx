import { useState } from 'react';
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
import { NewEntitlementDialog } from '../features/plans/components/NewEntitlementDialog';
import { useEntitlements } from '../features/plans/hooks/usePlans';
import { useCreateEntitlement } from '../features/plans/hooks/useEntitlementMutations';
import { getApiErrorMessage } from '../utils/apiError';

export function EntitlementsPage() {
  const { data: entitlements = [], isLoading, isError } = useEntitlements();
  const createEntitlement = useCreateEntitlement();
  const [isNewOpen, setIsNewOpen] = useState(false);

  return (
    <DashboardLayout>
      <Card>
        <Stack
          direction="row"
          sx={{ alignItems: 'center', justifyContent: 'space-between', px: 3, pt: 3, pb: 2 }}
        >
          <Box>
            <Typography variant="h6">Entitlements</Typography>
            <Typography variant="body2" color="text.secondary">
              Feature flags and numeric limits that plans can configure.
            </Typography>
          </Box>
          <Button
            variant="contained"
            onClick={() => {
              createEntitlement.reset();
              setIsNewOpen(true);
            }}
          >
            New entitlement
          </Button>
        </Stack>

        {isError && (
          <Alert severity="error" sx={{ mx: 3, mb: 2 }}>
            Failed to load entitlements. Please try again.
          </Alert>
        )}

        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Key</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Description</TableCell>
                <TableCell>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading &&
                Array.from({ length: 3 }, (_, i) => (
                  <TableRow key={i}>
                    <TableCell><Skeleton variant="text" width={120} /></TableCell>
                    <TableCell><Skeleton variant="rounded" width={60} height={24} /></TableCell>
                    <TableCell><Skeleton variant="text" width={180} /></TableCell>
                    <TableCell><Skeleton variant="rounded" width={52} height={24} /></TableCell>
                  </TableRow>
                ))}

              {!isLoading &&
                entitlements.map((e) => (
                  <TableRow key={e.id}>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 500 }}>
                        {e.key}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip label={e.type} size="small" variant="outlined" />
                    </TableCell>
                    <TableCell>{e.description ?? '—'}</TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={e.isActive ? 'Active' : 'Inactive'}
                        color={e.isActive ? 'success' : 'warning'}
                      />
                    </TableCell>
                  </TableRow>
                ))}

              {!isLoading && entitlements.length === 0 && (
                <TableRow>
                  <TableCell colSpan={4} align="center" sx={{ py: 6 }}>
                    <Typography variant="subtitle1">No entitlements yet</Typography>
                    <Typography variant="body2" color="text.secondary">
                      Define your first feature flag or limit.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>

      <NewEntitlementDialog
        open={isNewOpen}
        saving={createEntitlement.isPending}
        errorMessage={getApiErrorMessage(createEntitlement.error, 'Failed to create entitlement.')}
        onClose={() => setIsNewOpen(false)}
        onCreate={(payload) => {
          createEntitlement.mutate(payload, { onSuccess: () => setIsNewOpen(false) });
        }}
      />
    </DashboardLayout>
  );
}
