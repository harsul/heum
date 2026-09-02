import { useState } from 'react';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import type { EntitlementType } from '../types/plan';

interface NewEntitlementDialogProps {
  open: boolean;
  saving: boolean;
  errorMessage?: string;
  onClose: () => void;
  onCreate: (payload: { key: string; type: EntitlementType; description?: string }) => void;
}

export function NewEntitlementDialog({
  open,
  saving,
  errorMessage,
  onClose,
  onCreate,
}: NewEntitlementDialogProps) {
  const [key, setKey] = useState('');
  const [type, setType] = useState<EntitlementType>('Integer');
  const [description, setDescription] = useState('');

  const handleClose = () => {
    setKey('');
    setType('Integer');
    setDescription('');
    onClose();
  };

  const isKeyValid = /^[a-z][a-z0-9_]*$/.test(key);

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="xs">
      <DialogTitle>New entitlement</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
          <TextField
            label="Key"
            value={key}
            onChange={(e) => setKey(e.target.value)}
            error={key.length > 0 && !isKeyValid}
            helperText={
              key.length > 0 && !isKeyValid
                ? 'Must be lowercase letters, digits, or underscores, starting with a letter.'
                : 'e.g. max_users'
            }
            autoFocus
            fullWidth
          />
          <FormControl fullWidth>
            <InputLabel>Type</InputLabel>
            <Select
              value={type}
              label="Type"
              onChange={(e) => setType(e.target.value as EntitlementType)}
            >
              <MenuItem value="Boolean">Boolean</MenuItem>
              <MenuItem value="Integer">Integer</MenuItem>
              <MenuItem value="Decimal">Decimal</MenuItem>
            </Select>
          </FormControl>
          <TextField
            label="Description (optional)"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={handleClose} disabled={saving} color="inherit">
          Cancel
        </Button>
        <Button
          variant="contained"
          disabled={!isKeyValid || saving}
          onClick={() =>
            onCreate({ key, type, description: description.trim() || undefined })
          }
        >
          Create entitlement
        </Button>
      </DialogActions>
    </Dialog>
  );
}
