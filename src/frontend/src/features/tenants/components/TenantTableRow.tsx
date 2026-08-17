import { useState } from 'react';
import Avatar from '@mui/material/Avatar';
import Checkbox from '@mui/material/Checkbox';
import Chip from '@mui/material/Chip';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import ListItemIcon from '@mui/material/ListItemIcon';
import Stack from '@mui/material/Stack';
import TableCell from '@mui/material/TableCell';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import MoreVertIcon from '@mui/icons-material/MoreVertOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import BlockOutlinedIcon from '@mui/icons-material/BlockOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined';
import type { Tenant } from '../types/tenant';
import { formatDate, tenantInitials } from '../utils';

interface TenantTableRowProps {
  tenant: Tenant;
  selected: boolean;
  onSelectRow: () => void;
  onEdit: () => void;
  onToggleActive: () => void;
  toggleActiveDisabled?: boolean;
}

export function TenantTableRow({
  tenant,
  selected,
  onSelectRow,
  onEdit,
  onToggleActive,
  toggleActiveDisabled,
}: TenantTableRowProps) {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  return (
    <TableRow selected={selected} tabIndex={-1}>
      <TableCell padding="checkbox">
        <Checkbox checked={selected} onChange={onSelectRow} />
      </TableCell>

      <TableCell component="th" scope="row">
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <Avatar sx={{ width: 36, height: 36, bgcolor: 'primary.main', fontSize: 14 }}>
            {tenantInitials(tenant.name)}
          </Avatar>
          <Typography variant="subtitle2" noWrap>
            {tenant.name}
          </Typography>
        </Stack>
      </TableCell>

      <TableCell>
        <Typography variant="body2" color="text.secondary">
          {tenant.slug}
        </Typography>
      </TableCell>

      <TableCell>{formatDate(tenant.createdAtUtc)}</TableCell>

      <TableCell>{formatDate(tenant.updatedAtUtc)}</TableCell>

      <TableCell align="center">
        <Chip
          size="small"
          label={tenant.isActive ? 'Active' : 'Inactive'}
          color={tenant.isActive ? 'success' : 'default'}
          variant={tenant.isActive ? 'filled' : 'outlined'}
        />
      </TableCell>

      <TableCell align="right">
        <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} aria-label="Row actions">
          <MoreVertIcon />
        </IconButton>

        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={() => setAnchorEl(null)}
          anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
          transformOrigin={{ horizontal: 'right', vertical: 'top' }}
          slotProps={{ paper: { sx: { width: 160 } } }}
        >
          <MenuItem
            onClick={() => {
              setAnchorEl(null);
              onEdit();
            }}
          >
            <ListItemIcon>
              <EditOutlinedIcon fontSize="small" />
            </ListItemIcon>
            Edit
          </MenuItem>
          <MenuItem
            onClick={() => {
              setAnchorEl(null);
              onToggleActive();
            }}
            disabled={toggleActiveDisabled}
            sx={{ color: tenant.isActive ? 'error.main' : 'success.main' }}
          >
            <ListItemIcon>
              {tenant.isActive ? (
                <BlockOutlinedIcon fontSize="small" color="error" />
              ) : (
                <CheckCircleOutlineIcon fontSize="small" color="success" />
              )}
            </ListItemIcon>
            {tenant.isActive ? 'Deactivate' : 'Reactivate'}
          </MenuItem>
        </Menu>
      </TableCell>
    </TableRow>
  );
}
