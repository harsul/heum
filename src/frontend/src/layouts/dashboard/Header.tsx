import { useState } from 'react';
import AppBar from '@mui/material/AppBar';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Toolbar from '@mui/material/Toolbar';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import MenuIcon from '@mui/icons-material/Menu';
import NotificationsNoneIcon from '@mui/icons-material/NotificationsNone';
import { useAuth } from 'react-oidc-context';
import { NAV_WIDTH } from './NavSidebar';

interface HeaderProps {
  onOpenNav: () => void;
}

export function Header({ onOpenNav }: HeaderProps) {
  const auth = useAuth();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const displayName =
    auth.user?.profile.preferred_username ?? auth.user?.profile.email ?? 'User';
  const initials = displayName.slice(0, 2).toUpperCase();

  return (
    <AppBar
      position="sticky"
      elevation={0}
      sx={{
        width: { md: `calc(100% - ${NAV_WIDTH}px)` },
        ml: { md: `${NAV_WIDTH}px` },
        backdropFilter: 'blur(12px)',
      }}
    >
      <Toolbar sx={{ gap: 1 }}>
        <IconButton
          onClick={onOpenNav}
          sx={{ display: { xs: 'inline-flex', md: 'none' }, mr: 1 }}
          aria-label="Open navigation menu"
        >
          <MenuIcon />
        </IconButton>

        <Box sx={{ flexGrow: 1 }} />

        <Tooltip title="Notifications">
          <IconButton color="inherit" aria-label="Notifications">
            <NotificationsNoneIcon />
          </IconButton>
        </Tooltip>

        <Tooltip title={displayName}>
          <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} sx={{ ml: 1 }}>
            <Avatar sx={{ width: 36, height: 36, bgcolor: 'secondary.main', fontSize: 14 }}>
              {initials}
            </Avatar>
          </IconButton>
        </Tooltip>

        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={() => setAnchorEl(null)}
          transformOrigin={{ horizontal: 'right', vertical: 'top' }}
          anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
        >
          <Box sx={{ px: 2, py: 1.5 }}>
            <Typography variant="subtitle2">{displayName}</Typography>
          </Box>
          <MenuItem
            onClick={() => {
              setAnchorEl(null);
              auth.signoutRedirect();
            }}
          >
            Log out
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
}
