import { useState } from 'react';
import Accordion from '@mui/material/Accordion';
import AccordionDetails from '@mui/material/AccordionDetails';
import AccordionSummary from '@mui/material/AccordionSummary';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Paper from '@mui/material/Paper';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TablePagination from '@mui/material/TablePagination';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import type { Entitlement, Plan, ResolvedEntitlements, TenantSubscription } from '../types/plan';
import { TenantOverridesTable } from './TenantOverridesTable';
import { formatDateTime } from '../../../utils/format';

interface CurrentPlanInfo {
  planName?: string | null;
  effectiveAtUtc?: string;
  notes?: string | null;
}

interface SubscriptionTabContentProps {
  currentPlan: CurrentPlanInfo | null;
  isLoading: boolean;
  /** When provided, renders the "Change plan" button (sysadmin only). */
  onChangePlan?: () => void;
  /** When provided, renders the subscription history accordion (sysadmin only). */
  subHistory?: TenantSubscription[];
  historyLoading?: boolean;
  /** When provided, renders the entitlement overrides accordion (sysadmin only). */
  entitlements?: ResolvedEntitlements;
  entitlementsLoading?: boolean;
  planDetails?: Plan | null;
  catalogEntitlements?: Entitlement[];
  tenantId?: string;
}

export function SubscriptionTabContent({
  currentPlan,
  isLoading,
  onChangePlan,
  subHistory,
  historyLoading,
  entitlements,
  entitlementsLoading,
  planDetails,
  catalogEntitlements,
  tenantId,
}: SubscriptionTabContentProps) {
  const [historyPage, setHistoryPage] = useState(0);
  const [historyRowsPerPage, setHistoryRowsPerPage] = useState(5);

  const hasAdminSections = subHistory !== undefined || entitlements !== undefined;

  return (
    <Box>
      <Paper variant="outlined" sx={{ p: 2.5, mb: hasAdminSections ? 3 : 0, borderRadius: 2 }}>
        <Stack
          direction="row"
          sx={{ alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2 }}
        >
          <Box>
            <Typography variant="overline" color="text.secondary" sx={{ lineHeight: 1, display: 'block' }}>
              Current plan
            </Typography>
            {isLoading ? (
              <>
                <Skeleton variant="text" width={160} height={40} />
                <Skeleton variant="text" width={200} sx={{ fontSize: '0.875rem' }} />
              </>
            ) : currentPlan?.planName ? (
              <>
                <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mt: 0.5 }}>
                  <Typography variant="h5" sx={{ fontWeight: 700 }}>
                    {currentPlan.planName}
                  </Typography>
                  <Chip label="Active" color="success" size="small" />
                </Stack>
                {currentPlan.effectiveAtUtc && (
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
                    Assigned {formatDateTime(currentPlan.effectiveAtUtc)}
                    {currentPlan.notes && ` · ${currentPlan.notes}`}
                  </Typography>
                )}
              </>
            ) : (
              <Typography variant="body1" sx={{ mt: 0.5 }} color="text.secondary">
                No plan assigned
              </Typography>
            )}
          </Box>
          {onChangePlan && (
            <Button variant="contained" onClick={onChangePlan}>
              Change plan
            </Button>
          )}
        </Stack>
      </Paper>

      {subHistory !== undefined && (
        <Accordion
          variant="outlined"
          sx={{ borderRadius: 2, '&:before': { display: 'none' }, mb: entitlements !== undefined ? 1 : 0 }}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
              Subscription history
            </Typography>
          </AccordionSummary>
          <AccordionDetails sx={{ pt: 0 }}>
            {historyLoading ? (
              <Skeleton variant="rectangular" height={80} sx={{ borderRadius: 1 }} />
            ) : (
              <>
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Plan</TableCell>
                        <TableCell>Reason</TableCell>
                        <TableCell>Notes</TableCell>
                        <TableCell>Effective</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {subHistory
                        .slice(historyPage * historyRowsPerPage, (historyPage + 1) * historyRowsPerPage)
                        .map((s) => (
                          <TableRow key={s.id} hover>
                            <TableCell sx={{ fontWeight: 500 }}>{s.planName}</TableCell>
                            <TableCell>{s.reason}</TableCell>
                            <TableCell>{s.notes ?? '—'}</TableCell>
                            <TableCell>{formatDateTime(s.effectiveAtUtc)}</TableCell>
                          </TableRow>
                        ))}
                      {subHistory.length === 0 && (
                        <TableRow>
                          <TableCell colSpan={4} align="center" sx={{ py: 3 }}>
                            <Typography variant="body2" color="text.secondary">
                              No subscription history.
                            </Typography>
                          </TableCell>
                        </TableRow>
                      )}
                    </TableBody>
                  </Table>
                </TableContainer>
                {subHistory.length > 0 && (
                  <TablePagination
                    component="div"
                    count={subHistory.length}
                    page={historyPage}
                    onPageChange={(_, p) => setHistoryPage(p)}
                    rowsPerPage={historyRowsPerPage}
                    onRowsPerPageChange={(e) => {
                      setHistoryRowsPerPage(+e.target.value);
                      setHistoryPage(0);
                    }}
                    rowsPerPageOptions={[5, 10, 25]}
                  />
                )}
              </>
            )}
          </AccordionDetails>
        </Accordion>
      )}

      {entitlements !== undefined && tenantId && (
        <Accordion variant="outlined" sx={{ borderRadius: 2, '&:before': { display: 'none' } }}>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
              Entitlement overrides
            </Typography>
          </AccordionSummary>
          <AccordionDetails sx={{ pt: 0 }}>
            <TenantOverridesTable
              tenantId={tenantId}
              entitlements={entitlements}
              planEntitlements={planDetails?.entitlements ?? []}
              catalogEntitlements={catalogEntitlements ?? []}
              isLoading={entitlementsLoading ?? false}
            />
          </AccordionDetails>
        </Accordion>
      )}
    </Box>
  );
}
