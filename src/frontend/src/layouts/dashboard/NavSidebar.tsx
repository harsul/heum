import Box from '@mui/material/Box';
import Drawer from '@mui/material/Drawer';
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
<List sx={{ px: 1.5, flex: 1 }}>
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
              '&:hover': { backgroundColor: 'rgba(0, 171, 85, 0.08)', color: 'text.primary' },
              '&.active': {
                backgroundColor: 'rgba(0, 171, 85, 0.12)',
                color: '#00AB55',
                '& .MuiListItemIcon-root': { color: '#00AB55' },
                '&:hover': { backgroundColor: 'rgba(0, 171, 85, 0.16)' },
              },
              '&.Mui-disabled': { opacity: 0.4 },
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

      <Box sx={{ px: 2, py: 1.5 }}>
        <Typography variant="caption" sx={{ color: 'text.secondary' }}>
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
          '& .MuiDrawer-paper': { width: NAV_WIDTH, boxSizing: 'border-box', borderRadius: '0 16px 16px 0' },
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
            width: NAV_WIDTH - 24,
            margin: '12px 0 12px 12px',
            height: 'calc(100vh - 24px)',
            borderRadius: '16px',
            boxSizing: 'border-box',
            overflowX: 'hidden',
          },
        }}
        open
      >
        <NavContent />
      </Drawer>
    </Box>
  );
}
