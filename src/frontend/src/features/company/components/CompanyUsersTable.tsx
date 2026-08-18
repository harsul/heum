import { useState } from 'react';
import { useAuth } from 'react-oidc-context';
import Alert from '@mui/material/Alert';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import CircularProgress from '@mui/material/CircularProgress';
import IconButton from '@mui/material/IconButton';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import AddOutlinedIcon from '@mui/icons-material/AddOutlined';
import BlockOutlinedIcon from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined';
import { formatDate, tenantInitials } from '../../../utils/format';
import { getApiErrorMessage } from '../../../utils/apiError';
import { AddUserByEmailDialog } from '../../../components/AddUserByEmailDialog';
import { useMyTenantUsers } from '../hooks/useMyTenantUsers';
import { useAddMyTenantUser } from '../hooks/useAddMyTenantUser';
import { useSetMyTenantUserEnabled } from '../hooks/useSetMyTenantUserEnabled';

export function CompanyUsersTable() {
  const auth = useAuth();
  const currentUserId = auth.user?.profile.sub;
  const { data: users = [], isLoading, isError } = useMyTenantUsers();
  const addUser = useAddMyTenantUser();
  const setUserEnabled = useSetMyTenantUserEnabled();
  const [isAddingUser, setIsAddingUser] = useState(false);

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: 'flex-end', mb: 2 }}>
        <Button
          variant="outlined"
          startIcon={<AddOutlinedIcon />}
          onClick={() => {
            addUser.reset();
            setIsAddingUser(true);
          }}
        >
          Add user
        </Button>
      </Stack>

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress size={28} />
        </Box>
      )}

      {isError && <Alert severity="error">Failed to load your team. Please try again.</Alert>}

      {!isLoading && !isError && setUserEnabled.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {getApiErrorMessage(setUserEnabled.error, 'Failed to update this user.')}
        </Alert>
      )}

      {!isLoading && !isError && users.length === 0 && (
        <Box sx={{ py: 6, textAlign: 'center' }}>
          <Typography variant="subtitle1">No users found</Typography>
          <Typography variant="body2" color="text.secondary">
            No teammates have been added to your company yet.
          </Typography>
        </Box>
      )}

      {!isLoading && !isError && users.length > 0 && (
        <TableContainer sx={{ overflow: 'unset' }}>
          <Table sx={{ minWidth: 640 }}>
            <TableHead>
              <TableRow>
                <TableCell>User</TableCell>
                <TableCell>Email</TableCell>
                <TableCell align="center">Status</TableCell>
                <TableCell align="center">Email verified</TableCell>
                <TableCell>Created</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {users.map((user) => {
                const displayName = [user.firstName, user.lastName].filter(Boolean).join(' ') || user.username;
                const isSelf = user.id === currentUserId;

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
                    <TableCell align="right">
                      <Tooltip
                        title={
                          isSelf
                            ? "You can't disable your own account"
                            : user.enabled
                              ? 'Disable user'
                              : 'Enable user'
                        }
                      >
                        <span>
                          <IconButton
                            size="small"
                            disabled={isSelf || setUserEnabled.isPending}
                            color={user.enabled ? 'error' : 'success'}
                            onClick={() =>
                              setUserEnabled.mutate({ userId: user.id, enabled: !user.enabled })
                            }
                          >
                            {user.enabled ? (
                              <BlockOutlinedIcon fontSize="small" />
                            ) : (
                              <CheckCircleOutlineIcon fontSize="small" />
                            )}
                          </IconButton>
                        </span>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <AddUserByEmailDialog
        open={isAddingUser}
        saving={addUser.isPending}
        errorMessage={getApiErrorMessage(addUser.error, 'Failed to add user.')}
        onClose={() => setIsAddingUser(false)}
        onAdd={(values) => {
          addUser.mutate(values, { onSuccess: () => setIsAddingUser(false) });
        }}
      />
    </Box>
  );
}
