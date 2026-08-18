import Box from '@mui/material/Box';
import Drawer from '@mui/material/Drawer';
import Link from '@mui/material/Link';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Typography from '@mui/material/Typography';
import { NavLink } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { getNavConfig } from './config-nav';
import { isSystemAdmin, isTenantAdmin } from '../../auth/roles';

export const NAV_WIDTH = 280;

interface NavSidebarProps {
  open: boolean;
  onClose: () => void;
}

function NavContent() {
  const auth = useAuth();
  const navConfig = getNavConfig(isSystemAdmin(auth.user), isTenantAdmin(auth.user));

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, px: 3, py: 3 }}>
        <Link
          href="https://aspire.dev"
          target="_blank"
          rel="noopener noreferrer"
          aria-label="Visit Aspire website (opens in new tab)"
          sx={{ display: 'flex', alignItems: 'center' }}
        >
          <Box component="img" src="/Aspire.png" alt="Aspire logo" sx={{ height: 32, width: 'auto' }} />
        </Link>
        <Typography variant="h6" sx={{ fontWeight: 700 }}>
          Heum
        </Typography>
      </Box>

      <List sx={{ px: 2, flex: 1 }}>
        {navConfig.map((item) => (
          <ListItemButton
            key={item.title}
            component={item.disabled ? 'div' : NavLink}
            to={item.disabled ? undefined : item.path}
            disabled={item.disabled}
            end
            sx={{
              mb: 0.5,
              borderRadius: 1.5,
              color: 'text.secondary',
              '& .MuiListItemIcon-root': { color: 'inherit' },
              '&.active': {
                bgcolor: (theme) => `${theme.palette.primary.main}1f`,
                color: 'primary.main',
                fontWeight: 700,
                '&:hover': { bgcolor: (theme) => `${theme.palette.primary.main}29` },
              },
            }}
          >
            <ListItemIcon sx={{ minWidth: 36 }}>
              <item.icon fontSize="small" />
            </ListItemIcon>
            <ListItemText
              primary={item.title}
              slotProps={{ primary: { sx: { fontSize: 14, fontWeight: 600 } } }}
            />
          </ListItemButton>
        ))}
      </List>

      <Box sx={{ px: 3, py: 2 }}>
        <Typography variant="caption" color="text.secondary">
          Powered by .NET Aspire
        </Typography>
      </Box>
    </Box>
  );
}

export function NavSidebar({ open, onClose }: NavSidebarProps) {
  return (
    <Box component="nav" sx={{ flexShrink: { md: 0 }, width: { md: NAV_WIDTH } }}>
      {/* Mobile drawer */}
      <Drawer
        open={open}
        onClose={onClose}
        variant="temporary"
        ModalProps={{ keepMounted: true }}
        sx={{
          display: { xs: 'block', md: 'none' },
          '& .MuiDrawer-paper': { width: NAV_WIDTH, boxSizing: 'border-box' },
        }}
      >
        <NavContent />
      </Drawer>

      {/* Desktop permanent drawer */}
      <Drawer
        variant="permanent"
        sx={{
          display: { xs: 'none', md: 'block' },
          '& .MuiDrawer-paper': {
            width: NAV_WIDTH,
            boxSizing: 'border-box',
            borderRight: '1px solid rgba(255, 255, 255, 0.08)',
          },
        }}
        open
      >
        <NavContent />
      </Drawer>
    </Box>
  );
}
