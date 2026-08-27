import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Skeleton from '@mui/material/Skeleton';
import Collapse from '@mui/material/Collapse';
import IconButton from '@mui/material/IconButton';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown';
import KeyboardArrowUpIcon from '@mui/icons-material/KeyboardArrowUp';
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

function changeSummary(entry: TenantHistoryEntry): string {
  const changes = diffAuditValues(entry.oldValues, entry.newValues);
  if (changes) {
    const count = changes.length;
    return `${count} field${count !== 1 ? 's' : ''} changed`;
  }
  if (entry.action === 'Insert') return 'Record created';
  if (entry.action === 'Delete') return 'Record deleted';
  return '—';
}

function HistoryRow({ entry }: { entry: TenantHistoryEntry }) {
  const [open, setOpen] = useState(false);
  const changes = diffAuditValues(entry.oldValues, entry.newValues);

  return (
    <>
      <TableRow
        hover
        onClick={() => setOpen((prev) => !prev)}
        sx={{ cursor: 'pointer', '& > *': { borderBottom: open ? 0 : undefined } }}
      >
        <TableCell sx={{ width: 48, pr: 0 }}>
          <IconButton size="small" onClick={(e) => { e.stopPropagation(); setOpen((prev) => !prev); }}>
            {open ? <KeyboardArrowUpIcon fontSize="small" /> : <KeyboardArrowDownIcon fontSize="small" />}
          </IconButton>
        </TableCell>
        <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDateTime(entry.timestampUtc)}</TableCell>
        <TableCell>
          <Chip size="small" label={entry.action} color={ACTION_COLOR[entry.action]} variant="outlined" />
        </TableCell>
        <TableCell>
          <Typography variant="body2" color="text.secondary">
            {changeSummary(entry)}
          </Typography>
        </TableCell>
        <TableCell>
          <Typography variant="body2" color="text.secondary" sx={{ fontFamily: 'monospace', fontSize: 12 }}>
            {entry.userId}
          </Typography>
        </TableCell>
      </TableRow>

      <TableRow>
        <TableCell colSpan={5} sx={{ py: 0, borderBottom: open ? undefined : 0 }}>
          <Collapse in={open} unmountOnExit>
            <Box sx={{ py: 2, pl: 6, pr: 2 }}>
              {changes ? (
                <Table size="small" sx={{ tableLayout: 'fixed' }}>
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ width: '20%', fontWeight: 600, color: 'text.secondary', border: 0 }}>Field</TableCell>
                      <TableCell sx={{ width: '40%', fontWeight: 600, color: '#CF222E', border: 0 }}>Before</TableCell>
                      <TableCell sx={{ width: '40%', fontWeight: 600, color: '#116329', border: 0 }}>After</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {changes.map(({ field, oldValue, newValue }) => (
                      <TableRow key={field} sx={{ '& td': { border: 0, py: 0.5 } }}>
                        <TableCell>
                          <Typography variant="caption" sx={{ fontWeight: 600, color: 'text.secondary', fontFamily: 'monospace' }}>
                            {field}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          {oldValue && (
                            <Box sx={{ px: 1.5, py: 0.5, bgcolor: '#FFEBE9', borderLeft: '3px solid #CF222E', color: '#CF222E', fontFamily: 'monospace', fontSize: 13 }}>
                              − {oldValue}
                            </Box>
                          )}
                        </TableCell>
                        <TableCell>
                          {newValue && (
                            <Box sx={{ px: 1.5, py: 0.5, bgcolor: '#E6FFEC', borderLeft: '3px solid #116329', color: '#116329', fontFamily: 'monospace', fontSize: 13 }}>
                              + {newValue}
                            </Box>
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              ) : (
                <Typography
                  variant="body2"
                  color="text.secondary"
                  component="pre"
                  sx={{ m: 0, fontFamily: 'monospace', whiteSpace: 'pre-wrap', fontSize: 12 }}
                >
                  {entry.newValues ?? entry.oldValues ?? '—'}
                </Typography>
              )}
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </>
  );
}

export function TenantHistoryTable({ tenantId }: TenantHistoryTableProps) {
  const { data, isLoading, isError, error, fetchNextPage, hasNextPage, isFetchingNextPage } =
    useTenantHistory(tenantId);
  const entries = data?.pages.flatMap((page) => page.items) ?? [];

  return (
    <Box>
      {isLoading && (
        <TableContainer sx={{ overflow: 'unset' }}>
          <Table sx={{ minWidth: 640 }}>
            <TableHead>
              <TableRow>
                <TableCell sx={{ width: 48 }} />
                <TableCell>Timestamp</TableCell>
                <TableCell>Action</TableCell>
                <TableCell>Summary</TableCell>
                <TableCell>User</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {Array.from({ length: 5 }, (_, i) => (
                <TableRow key={i}>
                  <TableCell sx={{ width: 48, pr: 0 }}>
                    <Skeleton variant="circular" width={24} height={24} />
                  </TableCell>
                  <TableCell><Skeleton variant="text" width={130} /></TableCell>
                  <TableCell><Skeleton variant="rounded" width={56} height={24} /></TableCell>
                  <TableCell><Skeleton variant="text" width={110} /></TableCell>
                  <TableCell><Skeleton variant="text" width={200} /></TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
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
            Changes to this tenant's details will show up here.
          </Typography>
        </Box>
      )}

      {!isLoading && !isError && entries.length > 0 && (
        <>
          <TableContainer sx={{ overflow: 'unset' }}>
            <Table sx={{ minWidth: 640 }}>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ width: 48 }} />
                  <TableCell>Timestamp</TableCell>
                  <TableCell>Action</TableCell>
                  <TableCell>Summary</TableCell>
                  <TableCell>User</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {entries.map((entry) => (
                  <HistoryRow key={entry.id} entry={entry} />
                ))}
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
