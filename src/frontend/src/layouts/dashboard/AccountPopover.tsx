import { useState } from 'react';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import IconButton from '@mui/material/IconButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Typography from '@mui/material/Typography';
import PersonOutlineIcon from '@mui/icons-material/PersonOutlineOutlined';
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined';
import LogoutIcon from '@mui/icons-material/Logout';
import { useAuth } from 'react-oidc-context';

export function AccountPopover() {
  const auth = useAuth();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const open = Boolean(anchorEl);

  const displayName =
    auth.user?.profile.name ?? auth.user?.profile.preferred_username ?? 'User';
  const email = auth.user?.profile.email;
  const picture = auth.user?.profile.picture;
  const initials = displayName.slice(0, 2).toUpperCase();

  const handleClose = () => setAnchorEl(null);

  return (
    <>
      <IconButton
        onClick={(e) => setAnchorEl(e.currentTarget)}
        sx={{
          ml: 1,
          p: 0.5,
          border: (theme) =>
            `2px solid ${open ? theme.palette.primary.main : 'transparent'}`,
          transition: 'border-color 0.2s ease',
        }}
        aria-label="Account menu"
        aria-controls={open ? 'account-menu' : undefined}
        aria-haspopup="true"
        aria-expanded={open ? 'true' : undefined}
      >
        <Avatar src={picture} sx={{ width: 36, height: 36, bgcolor: 'secondary.main', fontSize: 14 }}>
          {!picture && initials}
        </Avatar>
      </IconButton>

      <Menu
        id="account-menu"
        anchorEl={anchorEl}
        open={open}
        onClose={handleClose}
        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
        slotProps={{
          paper: {
            elevation: 0,
            sx: {
              mt: 1.5,
              width: 260,
              overflow: 'visible',
              border: '1px solid rgba(145, 158, 171, 0.16)',
              boxShadow: '0 0 2px 0 rgba(145,158,171,0.20), 0 12px 24px -4px rgba(145,158,171,0.12)',
              '&::before': {
                content: '""',
                position: 'absolute',
                top: 0,
                right: 18,
                width: 10,
                height: 10,
                bgcolor: 'background.paper',
                borderTop: '1px solid rgba(145, 158, 171, 0.16)',
                borderLeft: '1px solid rgba(145, 158, 171, 0.16)',
                transform: 'translateY(-50%) rotate(45deg)',
              },
            },
          },
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, px: 2, py: 1.5 }}>
          <Avatar src={picture} sx={{ width: 40, height: 40, bgcolor: 'secondary.main' }}>
            {!picture && initials}
          </Avatar>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="subtitle2" noWrap>
              {displayName}
            </Typography>
            {email ? (
              <Typography variant="body2" color="text.secondary" noWrap>
                {email}
              </Typography>
            ) : null}
          </Box>
        </Box>

        <Divider sx={{ borderStyle: 'dashed' }} />

        <MenuItem onClick={handleClose} sx={{ mx: 1, my: 0.5, borderRadius: 1 }} disabled>
          <ListItemIcon>
            <PersonOutlineIcon fontSize="small" />
          </ListItemIcon>
          Profile
        </MenuItem>

        <MenuItem onClick={handleClose} sx={{ mx: 1, my: 0.5, borderRadius: 1 }} disabled>
          <ListItemIcon>
            <SettingsOutlinedIcon fontSize="small" />
          </ListItemIcon>
          Settings
        </MenuItem>

        <Divider sx={{ borderStyle: 'dashed' }} />

        <MenuItem
          onClick={() => {
            handleClose();
            auth.signoutRedirect();
          }}
          sx={{ mx: 1, my: 0.5, borderRadius: 1, color: 'error.main' }}
        >
          <ListItemIcon>
            <LogoutIcon fontSize="small" color="error" />
          </ListItemIcon>
          Log out
        </MenuItem>
      </Menu>
    </>
  );
}
