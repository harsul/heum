import { useState } from 'react';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import { Header } from './Header';
import { NavSidebar, NAV_WIDTH } from './NavSidebar';

interface DashboardLayoutProps {
  children: React.ReactNode;
}

export function DashboardLayout({ children }: DashboardLayoutProps) {
  const [navOpen, setNavOpen] = useState(false);

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <NavSidebar open={navOpen} onClose={() => setNavOpen(false)} />

      <Box sx={{ flexGrow: 1, width: { md: `calc(100% - ${NAV_WIDTH}px)` } }}>
        <Header onOpenNav={() => setNavOpen(true)} />

        <Box component="main" sx={{ py: 4 }}>
          <Container maxWidth="xl">{children}</Container>
        </Box>
      </Box>
    </Box>
  );
}
