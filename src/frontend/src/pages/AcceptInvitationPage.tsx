import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CircularProgress from '@mui/material/CircularProgress';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined';
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
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        p: 3,
        bgcolor: 'background.default',
      }}
    >
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 4 }}>
        <Box
          sx={{
            width: 40,
            height: 40,
            borderRadius: 1.5,
            bgcolor: 'primary.main',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.contrastText',
            fontWeight: 800,
            fontSize: 20,
            fontFamily: 'Public Sans, sans-serif',
          }}
        >
          H
        </Box>
        <Typography variant="h5" sx={{ fontWeight: 800, letterSpacing: '-0.5px' }}>
          Heum
        </Typography>
      </Stack>

      <Card sx={{ maxWidth: 420, width: '100%' }}>
        <CardContent sx={{ p: 4 }}>
          {state === 'loading' && (
            <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center' }}>
              <CircularProgress />
              <Typography variant="body1" color="text.secondary">
                Accepting your invitation…
              </Typography>
            </Stack>
          )}

          {state === 'success' && (
            <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center' }}>
              <Box
                sx={{
                  width: 64,
                  height: 64,
                  borderRadius: '50%',
                  bgcolor: 'success.lighter',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <CheckCircleOutlineIcon sx={{ fontSize: 36, color: 'success.main' }} />
              </Box>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                You're in!
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Your account has been set up. Sign in to get started.
              </Typography>
              <Button
                variant="contained"
                fullWidth
                href="/"
                sx={{ mt: 1 }}
              >
                Sign in
              </Button>
            </Stack>
          )}

          {state === 'error' && (
            <Stack spacing={2}>
              <Alert severity="error">{errorMessage}</Alert>
              <Button variant="outlined" fullWidth onClick={() => window.location.reload()}>
                Try again
              </Button>
            </Stack>
          )}

          {state === 'missing-token' && (
            <Alert severity="warning">
              No invitation token found. Please use the link from your invitation email.
            </Alert>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
