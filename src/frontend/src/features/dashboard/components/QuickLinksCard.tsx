import { useNavigate } from 'react-router-dom';
import Card from '@mui/material/Card';
import CardHeader from '@mui/material/CardHeader';
import CardContent from '@mui/material/CardContent';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Typography from '@mui/material/Typography';
import Divider from '@mui/material/Divider';
import PeopleAltIcon from '@mui/icons-material/PeopleAltOutlined';
import LayersIcon from '@mui/icons-material/LayersOutlined';
import TuneIcon from '@mui/icons-material/TuneOutlined';
import AddBusinessIcon from '@mui/icons-material/AddBusiness';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';

const links = [
  { label: 'Manage Tenants', description: 'View and manage all tenants', path: '/tenants', icon: PeopleAltIcon },
  { label: 'Manage Plans', description: 'Configure subscription plans', path: '/admin/plans', icon: LayersIcon },
  { label: 'Entitlements', description: 'Define feature entitlements', path: '/admin/entitlements', icon: TuneIcon },
];

export function QuickLinksCard() {
  const navigate = useNavigate();

  return (
    <Card sx={{ height: '100%' }}>
      <CardHeader
        title="Quick Links"
        subheader="Jump to key sections"
        avatar={<AddBusinessIcon color="action" />}
      />
      <CardContent sx={{ pt: 0 }}>
        <List disablePadding>
          {links.map((link, index) => (
            <div key={link.path}>
              {index > 0 && <Divider />}
              <ListItemButton onClick={() => navigate(link.path)} sx={{ borderRadius: 1, py: 1.25 }}>
                <ListItemIcon sx={{ minWidth: 40 }}>
                  <link.icon fontSize="small" color="primary" />
                </ListItemIcon>
                <ListItemText
                  primary={
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>{link.label}</Typography>
                  }
                  secondary={
                    <Typography variant="caption">{link.description}</Typography>
                  }
                />
                <ChevronRightIcon fontSize="small" sx={{ color: 'text.disabled' }} />
              </ListItemButton>
            </div>
          ))}
        </List>
      </CardContent>
    </Card>
  );
}
