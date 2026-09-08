import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import IconButton from '@mui/material/IconButton';
import InputAdornment from '@mui/material/InputAdornment';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import BlockOutlinedIcon from '@mui/icons-material/BlockOutlined';
import SearchIcon from '@mui/icons-material/Search';
import { formatDate } from '../../../utils/format';
import { getApiErrorMessage } from '../../../utils/apiError';
import { useDebounce } from '../../../hooks/useDebounce';
import { useInvitations } from '../hooks/useInvitations';
import { useCreateInvitation, useRevokeInvitation } from '../hooks/useInvitationMutations';
import { InviteUserDialog } from './InviteUserDialog';

const STATUS_COLOR: Record<string, 'default' | 'warning' | 'success' | 'error'> = {
  Pending: 'warning',
  Accepted: 'success',
  Revoked: 'error',
  Expired: 'default',
};

export function InvitationsTable() {
  const [search, setSearch] = useState('');
  const [isInviting, setIsInviting] = useState(false);
  const debouncedSearch = useDebounce(search, 300);

  const { data, isLoading, isError, error, fetchNextPage, hasNextPage, isFetchingNextPage } =
    useInvitations(debouncedSearch || undefined);

  const createInvitation = useCreateInvitation();
  const revokeInvitation = useRevokeInvitation();

  const invitations = data?.pages.flatMap((p) => p.items) ?? [];

  return (
    <Box>
      <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', mb: 2 }}>
        <TextField
          size="small"
          placeholder="Search by email…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            },
          }}
          sx={{ width: 260 }}
        />
        <Button
          variant="contained"
          onClick={() => {
            createInvitation.reset();
            setIsInviting(true);
          }}
        >
          Invite user
        </Button>
      </Stack>

      {revokeInvitation.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getApiErrorMessage(revokeInvitation.error, 'Failed to revoke invitation.')}
        </Alert>
      )}

      {isLoading && (
        <TableContainer sx={{ overflow: 'unset' }}>
          <Table sx={{ minWidth: 580 }}>
            <TableHead>
              <TableRow>
                <TableCell>Email</TableCell>
                <TableCell align="center">Status</TableCell>
                <TableCell>Sent</TableCell>
                <TableCell>Expires</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {Array.from({ length: 5 }, (_, i) => (
                <TableRow key={i}>
                  <TableCell><Skeleton variant="text" width={180} /></TableCell>
                  <TableCell align="center"><Skeleton variant="rounded" width={70} height={24} /></TableCell>
                  <TableCell><Skeleton variant="text" width={90} /></TableCell>
                  <TableCell><Skeleton variant="text" width={90} /></TableCell>
                  <TableCell align="right"><Skeleton variant="circular" width={28} height={28} /></TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {isError && (
        <Alert severity="error">
          {getApiErrorMessage(error, 'Failed to load invitations. Please try again.')}
        </Alert>
      )}

      {!isLoading && !isError && invitations.length === 0 && (
        <Box sx={{ py: 6, textAlign: 'center' }}>
          <Typography variant="subtitle1">No invitations</Typography>
          <Typography variant="body2" color="text.secondary">
            Invite teammates to join your company.
          </Typography>
        </Box>
      )}

      {!isLoading && !isError && invitations.length > 0 && (
        <>
          <TableContainer sx={{ overflow: 'unset' }}>
            <Table sx={{ minWidth: 580 }}>
              <TableHead>
                <TableRow>
                  <TableCell>Email</TableCell>
                  <TableCell align="center">Status</TableCell>
                  <TableCell>Sent</TableCell>
                  <TableCell>Expires</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {invitations.map((inv) => (
                  <TableRow key={inv.id}>
                    <TableCell>
                      <Typography variant="body2">{inv.email}</Typography>
                    </TableCell>
                    <TableCell align="center">
                      <Chip
                        size="small"
                        label={inv.status}
                        color={STATUS_COLOR[inv.status] ?? 'default'}
                        variant={inv.status === 'Pending' ? 'filled' : 'outlined'}
                      />
                    </TableCell>
                    <TableCell>{formatDate(inv.createdAtUtc)}</TableCell>
                    <TableCell>{formatDate(inv.expiresAtUtc)}</TableCell>
                    <TableCell align="right">
                      <Tooltip
                        title={inv.status === 'Pending' ? 'Revoke invitation' : 'Already resolved'}
                      >
                        <span>
                          <IconButton
                            size="small"
                            color="error"
                            disabled={inv.status !== 'Pending' || revokeInvitation.isPending}
                            onClick={() => revokeInvitation.mutate(inv.id)}
                          >
                            <BlockOutlinedIcon fontSize="small" />
                          </IconButton>
                        </span>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
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

      <InviteUserDialog
        open={isInviting}
        saving={createInvitation.isPending}
        errorMessage={getApiErrorMessage(createInvitation.error, 'Failed to send invitation.')}
        onClose={() => setIsInviting(false)}
        onInvite={(email) => {
          createInvitation.mutate(email, { onSuccess: () => setIsInviting(false) });
        }}
      />
    </Box>
  );
}
