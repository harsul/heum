import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { acceptInvitation } from '../features/invitations/api/invitationsApi';
import { getApiErrorMessage } from '../utils/apiError';

type State = 'loading' | 'success' | 'error' | 'missing-token';

export function AcceptInvitationPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const [state, setState] = useState<State>(token ? 'loading' : 'missing-token');
  const [errorMessage, setErrorMessage] = useState<string>();

  useEffect(() => {
    if (!token) return;

    acceptInvitation(token)
      .then(() => setState('success'))
      .catch((err) => {
        setErrorMessage(getApiErrorMessage(err, 'The invitation link is invalid or has expired.'));
        setState('error');
      });
  }, [token]);

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        p: 3,
      }}
    >
      <Stack spacing={2} sx={{ maxWidth: 420, width: '100%', textAlign: 'center' }}>
        {state === 'loading' && (
          <>
            <CircularProgress sx={{ mx: 'auto' }} />
            <Typography variant="body1" color="text.secondary">
              Accepting your invitation…
            </Typography>
          </>
        )}

        {state === 'success' && (
          <>
            <Typography variant="h5" sx={{ fontWeight: 700 }}>
              Invitation accepted
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Your account is ready. You can now sign in.
            </Typography>
          </>
        )}

        {state === 'error' && (
          <Alert severity="error">{errorMessage}</Alert>
        )}

        {state === 'missing-token' && (
          <Alert severity="warning">
            No invitation token found. Please use the link from your invitation email.
          </Alert>
        )}
      </Stack>
    </Box>
  );
}
