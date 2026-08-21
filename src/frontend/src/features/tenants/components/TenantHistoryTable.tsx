import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import CircularProgress from '@mui/material/CircularProgress';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import { diffAuditValues, formatDateTime } from '../../../utils/format';
import { getApiErrorMessage } from '../../../utils/apiError';
import { useTenantHistory } from '../hooks/useTenantHistory';
import type { TenantHistoryEntry } from '../types/tenant';

interface TenantHistoryTableProps {
  tenantId: string;
}

const ACTION_COLOR: Record<TenantHistoryEntry['action'], 'success' | 'info' | 'error'> = {
  Insert: 'success',
  Update: 'info',
  Delete: 'error',
};

export function TenantHistoryTable({ tenantId }: TenantHistoryTableProps) {
  const { data, isLoading, isError, error, fetchNextPage, hasNextPage, isFetchingNextPage } =
    useTenantHistory(tenantId);
  const entries = data?.pages.flatMap((page) => page.items) ?? [];

  return (
    <Box>
      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress size={28} />
        </Box>
      )}

      {isError && (
        <Alert severity="error">
          {getApiErrorMessage(error, 'Failed to load tenant history. Please try again.')}
        </Alert>
      )}

      {!isLoading && !isError && entries.length === 0 && (
        <Box sx={{ py: 6, textAlign: 'center' }}>
          <Typography variant="subtitle1">No history yet</Typography>
          <Typography variant="body2" color="text.secondary">
            Changes to this tenant's details will show up here. User management actions
            (invites, enable/disable) aren't tracked in this history.
          </Typography>
        </Box>
      )}

      {!isLoading && !isError && entries.length > 0 && (
        <>
          <TableContainer sx={{ overflow: 'unset' }}>
            <Table sx={{ minWidth: 640 }}>
              <TableHead>
                <TableRow>
                  <TableCell>Timestamp</TableCell>
                  <TableCell>Action</TableCell>
                  <TableCell>Changes</TableCell>
                  <TableCell>User</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {entries.map((entry) => {
                  const changes = diffAuditValues(entry.oldValues, entry.newValues);

                  return (
                    <TableRow key={entry.id}>
                      <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDateTime(entry.timestampUtc)}</TableCell>
                      <TableCell>
                        <Chip size="small" label={entry.action} color={ACTION_COLOR[entry.action]} variant="outlined" />
                      </TableCell>
                      <TableCell>
                        {changes ? (
                          <Stack spacing={0.5}>
                            {changes.map(({ field, oldValue, newValue }) => (
                              <Typography key={field} variant="body2">
                                <strong>{field}</strong>: {oldValue} → {newValue}
                              </Typography>
                            ))}
                          </Stack>
                        ) : (
                          <Typography
                            variant="body2"
                            color="text.secondary"
                            component="pre"
                            sx={{ m: 0, fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}
                          >
                            {entry.newValues ?? entry.oldValues ?? '—'}
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>{entry.userId}</TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>

          {hasNextPage && (
            <Stack direction="row" sx={{ justifyContent: 'center', mt: 2 }}>
              <Button variant="outlined" onClick={() => fetchNextPage()} disabled={isFetchingNextPage}>
                {isFetchingNextPage ? 'Loading…' : 'Load more'}
              </Button>
            </Stack>
          )}
        </>
      )}
    </Box>
  );
}
