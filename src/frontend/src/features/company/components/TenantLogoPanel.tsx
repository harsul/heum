import { useRef, useState } from 'react';
import Alert from '@mui/material/Alert';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import type { Tenant } from '../../tenants/types/tenant';
import { useRemoveLogo, useUploadLogo } from '../hooks/useTenantLogo';
import { tenantInitials } from '../../../utils/format';
import { getApiErrorMessage } from '../../../utils/apiError';

const MAX_FILE_SIZE_BYTES = 2 * 1024 * 1024; // 2 MB

interface TenantLogoPanelProps {
  tenant: Tenant;
}

export function TenantLogoPanel({ tenant }: TenantLogoPanelProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [sizeError, setSizeError] = useState<string | null>(null);
  const uploadLogo = useUploadLogo();
  const removeLogo = useRemoveLogo();

  const isPending = uploadLogo.isPending || removeLogo.isPending;

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setSizeError(null);
    uploadLogo.reset();
    removeLogo.reset();

    if (file.size > MAX_FILE_SIZE_BYTES) {
      setSizeError('File is too large. Maximum size is 2 MB.');
      e.target.value = '';
      return;
    }

    uploadLogo.mutate(file, {
      onSettled: () => {
        e.target.value = '';
      },
    });
  }

  return (
    <Box>
      <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
        Company Logo
      </Typography>

      <Stack direction="row" spacing={3} sx={{ alignItems: 'center' }}>
        <Avatar
          src={tenant.logoUrl ?? undefined}
          sx={{ width: 72, height: 72, bgcolor: 'primary.main', fontSize: 24 }}
        >
          {!tenant.logoUrl && tenantInitials(tenant.name)}
        </Avatar>

        <Stack spacing={1}>
          <Stack direction="row" spacing={1}>
            <Button
              variant="outlined"
              size="small"
              disabled={isPending}
              onClick={() => fileInputRef.current?.click()}
              startIcon={isPending && uploadLogo.isPending ? <CircularProgress size={14} /> : undefined}
            >
              {tenant.logoUrl ? 'Replace logo' : 'Upload logo'}
            </Button>

            {tenant.logoUrl && (
              <Button
                variant="outlined"
                size="small"
                color="error"
                disabled={isPending}
                onClick={() => {
                  uploadLogo.reset();
                  removeLogo.mutate();
                }}
                startIcon={isPending && removeLogo.isPending ? <CircularProgress size={14} /> : undefined}
              >
                Remove
              </Button>
            )}
          </Stack>

          <Typography variant="caption" color="text.secondary">
            JPEG or PNG, max 2 MB.
          </Typography>
        </Stack>
      </Stack>

      <input
        ref={fileInputRef}
        type="file"
        accept="image/jpeg,image/png"
        style={{ display: 'none' }}
        onChange={handleFileChange}
      />

      {sizeError && (
        <Alert severity="error" sx={{ mt: 2, maxWidth: 400 }}>
          {sizeError}
        </Alert>
      )}
      {uploadLogo.isError && (
        <Alert severity="error" sx={{ mt: 2, maxWidth: 400 }}>
          {getApiErrorMessage(uploadLogo.error, 'Failed to upload logo.')}
        </Alert>
      )}
      {removeLogo.isError && (
        <Alert severity="error" sx={{ mt: 2, maxWidth: 400 }}>
          {getApiErrorMessage(removeLogo.error, 'Failed to remove logo.')}
        </Alert>
      )}
      {uploadLogo.isSuccess && (
        <Alert severity="success" sx={{ mt: 2, maxWidth: 400 }}>
          Logo updated.
        </Alert>
      )}
    </Box>
  );
}
