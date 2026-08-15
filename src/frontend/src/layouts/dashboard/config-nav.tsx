import DashboardIcon from '@mui/icons-material/DashboardOutlined';
import CloudQueueIcon from '@mui/icons-material/CloudQueueOutlined';
import PeopleAltIcon from '@mui/icons-material/PeopleAltOutlined';
import ShoppingCartIcon from '@mui/icons-material/ShoppingCartOutlined';
import BarChartIcon from '@mui/icons-material/BarChartOutlined';
import SettingsIcon from '@mui/icons-material/SettingsOutlined';
import type { SvgIconComponent } from '@mui/icons-material';

export interface NavItemConfig {
  title: string;
  path: string;
  icon: SvgIconComponent;
  /** Set to true for items that are not yet wired up to a route. */
  disabled?: boolean;
}

export const navConfig: NavItemConfig[] = [
  { title: 'Dashboard', path: '/', icon: DashboardIcon },
  // TODO: remove once the weather sample feature is no longer needed.
  { title: 'Weather', path: '/weather', icon: CloudQueueIcon },
  { title: 'Users', path: '/users', icon: PeopleAltIcon, disabled: true },
  { title: 'Orders', path: '/orders', icon: ShoppingCartIcon, disabled: true },
  { title: 'Analytics', path: '/analytics', icon: BarChartIcon, disabled: true },
  { title: 'Settings', path: '/settings', icon: SettingsIcon, disabled: true },
];
