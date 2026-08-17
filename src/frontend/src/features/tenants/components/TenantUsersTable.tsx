import Alert from '@mui/material/Alert';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
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
import { useTenantUsers } from '../hooks/useTenantUsers';
import { formatDate, tenantInitials } from '../utils';

interface TenantUsersTableProps {
  tenantId: string;
}

export function TenantUsersTable({ tenantId }: TenantUsersTableProps) {
  const { data: users = [], isLoading, isError } = useTenantUsers(tenantId);

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress size={28} />
      </Box>
    );
  }

  if (isError) {
    return <Alert severity="error">Failed to load users from Keycloak. Please try again.</Alert>;
  }

  if (users.length === 0) {
    return (
      <Box sx={{ py: 6, textAlign: 'center' }}>
        <Typography variant="subtitle1">No users found</Typography>
        <Typography variant="body2" color="text.secondary">
          No Keycloak users are associated with this tenant yet.
        </Typography>
      </Box>
    );
  }

  return (
    <TableContainer sx={{ overflow: 'unset' }}>
      <Table sx={{ minWidth: 640 }}>
        <TableHead>
          <TableRow>
            <TableCell>User</TableCell>
            <TableCell>Email</TableCell>
            <TableCell align="center">Status</TableCell>
            <TableCell align="center">Email verified</TableCell>
            <TableCell>Created</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {users.map((user) => {
            const displayName = [user.firstName, user.lastName].filter(Boolean).join(' ') || user.username;

            return (
              <TableRow key={user.id}>
                <TableCell component="th" scope="row">
                  <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
                    <Avatar sx={{ width: 32, height: 32, bgcolor: 'secondary.main', fontSize: 13 }}>
                      {tenantInitials(displayName)}
                    </Avatar>
                    <Box>
                      <Typography variant="subtitle2" noWrap>
                        {displayName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {user.username}
                      </Typography>
                    </Box>
                  </Stack>
                </TableCell>
                <TableCell>
                  <Typography variant="body2" color="text.secondary">
                    {user.email ?? '—'}
                  </Typography>
                </TableCell>
                <TableCell align="center">
                  <Chip
                    size="small"
                    label={user.enabled ? 'Enabled' : 'Disabled'}
                    color={user.enabled ? 'success' : 'default'}
                    variant={user.enabled ? 'filled' : 'outlined'}
                  />
                </TableCell>
                <TableCell align="center">
                  <Chip
                    size="small"
                    label={user.emailVerified ? 'Verified' : 'Unverified'}
                    color={user.emailVerified ? 'success' : 'warning'}
                    variant="outlined"
                  />
                </TableCell>
                <TableCell>{formatDate(user.createdAtUtc)}</TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
