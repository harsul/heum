import { useEffect, useState } from 'react';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControlLabel from '@mui/material/FormControlLabel';
import Stack from '@mui/material/Stack';
import Switch from '@mui/material/Switch';
import TextField from '@mui/material/TextField';
import type { Tenant } from '../types/tenant';

interface EditTenantDialogProps {
  tenant: Tenant | null;
  open: boolean;
  saving: boolean;
  onClose: () => void;
  onSave: (values: { name: string; isActive: boolean }) => void;
}

export function EditTenantDialog({ tenant, open, saving, onClose, onSave }: EditTenantDialogProps) {
  const [name, setName] = useState('');
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    if (tenant) {
      setName(tenant.name);
      setIsActive(tenant.isActive);
    }
  }, [tenant]);

  const isNameValid = name.trim().length >= 2;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Edit tenant</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField
            label="Name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            error={!isNameValid}
            helperText={isNameValid ? ' ' : 'Name must be at least 2 characters.'}
            autoFocus
            fullWidth
          />
          <FormControlLabel
            control={<Switch checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />}
            label={isActive ? 'Active' : 'Inactive'}
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={onClose} disabled={saving} color="inherit">
          Cancel
        </Button>
        <Button
          variant="contained"
          disabled={!isNameValid || saving}
          onClick={() => onSave({ name: name.trim(), isActive })}
        >
          Save
        </Button>
      </DialogActions>
    </Dialog>
  );
}
