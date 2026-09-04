import DashboardIcon from '@mui/icons-material/DashboardOutlined';
import PeopleAltIcon from '@mui/icons-material/PeopleAltOutlined';
import BusinessIcon from '@mui/icons-material/BusinessOutlined';
import LayersIcon from '@mui/icons-material/LayersOutlined';
import TuneIcon from '@mui/icons-material/TuneOutlined';
import type { SvgIconComponent } from '@mui/icons-material';

export interface NavItemConfig {
  title: string;
  path: string;
  icon: SvgIconComponent;
  /** Set to true for items that are not yet wired up to a route. */
  disabled?: boolean;
}

/**
 * Tenants management is only relevant (and only authorized) for system admins, and "My
 * Company" is only relevant (and only authorized) for tenant admins, so both are added
 * conditionally rather than being part of the static nav list.
 */
export function getNavConfig(isSystemAdmin: boolean, isTenantAdmin: boolean): NavItemConfig[] {
  return [
    { title: 'Dashboard', path: '/', icon: DashboardIcon },
    ...(isSystemAdmin
      ? [
          { title: 'Tenants', path: '/tenants', icon: PeopleAltIcon },
          { title: 'Plans', path: '/admin/plans', icon: LayersIcon },
          { title: 'Entitlements', path: '/admin/entitlements', icon: TuneIcon },
        ]
      : []),
    ...(isTenantAdmin ? [{ title: 'My Company', path: '/company', icon: BusinessIcon }] : [])
  ];
}
