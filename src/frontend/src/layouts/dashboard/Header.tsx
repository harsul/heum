import AppBar from '@mui/material/AppBar';
import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';
import Toolbar from '@mui/material/Toolbar';
import Tooltip from '@mui/material/Tooltip';
import MenuIcon from '@mui/icons-material/Menu';
import NotificationsNoneIcon from '@mui/icons-material/NotificationsNone';
import { NAV_WIDTH } from './NavSidebar';
import { AccountPopover } from './AccountPopover';

interface HeaderProps {
  onOpenNav: () => void;
}

export function Header({ onOpenNav }: HeaderProps) {
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

        <AccountPopover />
      </Toolbar>
    </AppBar>
  );
}
