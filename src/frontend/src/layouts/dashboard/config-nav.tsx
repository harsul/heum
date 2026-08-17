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

/**
 * Tenants management is only relevant (and only authorized) for system admins,
 * so it's added conditionally rather than being part of the static nav list.
 */
export function getNavConfig(isSystemAdmin: boolean): NavItemConfig[] {
  return [
    { title: 'Dashboard', path: '/', icon: DashboardIcon },
    // TODO: remove once the weather sample feature is no longer needed.
    { title: 'Weather', path: '/weather', icon: CloudQueueIcon },
    ...(isSystemAdmin ? [{ title: 'Tenants', path: '/tenants', icon: PeopleAltIcon }] : []),
    { title: 'Orders', path: '/orders', icon: ShoppingCartIcon, disabled: true },
    { title: 'Analytics', path: '/analytics', icon: BarChartIcon, disabled: true },
    { title: 'Settings', path: '/settings', icon: SettingsIcon, disabled: true },
  ];
}
