import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

interface InviteUserDialogProps {
  open: boolean;
  saving: boolean;
  errorMessage?: string;
  onClose: () => void;
  onInvite: (email: string) => void;
}

export function InviteUserDialog({ open, saving, errorMessage, onClose, onInvite }: InviteUserDialogProps) {
  const [email, setEmail] = useState('');

  const handleClose = () => {
    setEmail('');
    onClose();
  };

  const isValid = /\S+@\S+\.\S+/.test(email);

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="xs">
      <DialogTitle>Invite user</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
          <Typography variant="body2" color="text.secondary">
            We&apos;ll email this address a secure link to accept the invitation and set up their
            account.
          </Typography>
          <TextField
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            error={email.length > 0 && !isValid}
            helperText={email.length > 0 && !isValid ? 'Enter a valid email address.' : ' '}
            autoFocus
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={handleClose} disabled={saving} color="inherit">
          Cancel
        </Button>
        <Button variant="contained" disabled={!isValid || saving} onClick={() => onInvite(email)}>
          Send invite
        </Button>
      </DialogActions>
    </Dialog>
  );
}
