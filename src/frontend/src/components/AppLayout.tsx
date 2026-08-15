import AppBar from '@mui/material/AppBar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Container from '@mui/material/Container';
import Link from '@mui/material/Link';
import Stack from '@mui/material/Stack';
import Toolbar from '@mui/material/Toolbar';
import Typography from '@mui/material/Typography';
import { useAuth } from 'react-oidc-context';

interface AppLayoutProps {
  children: React.ReactNode;
}

export function AppLayout({ children }: AppLayoutProps) {
  const auth = useAuth();
  const displayName =
    auth.user?.profile.preferred_username ??
    auth.user?.profile.email ??
    'User';

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="sticky" elevation={0} sx={{ backdropFilter: 'blur(12px)' }}>
        <Toolbar>
          <Link
            href="https://aspire.dev"
            target="_blank"
            rel="noopener noreferrer"
            aria-label="Visit Aspire website (opens in new tab)"
            sx={{ display: 'flex', alignItems: 'center', mr: 2 }}
          >
            <Box
              component="img"
              src="/Aspire.png"
              alt="Aspire logo"
              sx={{ height: 36, width: 'auto' }}
            />
          </Link>

          <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 700 }}>
            Heum
          </Typography>

          <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
            <Typography
              variant="body2"
              color="inherit"
              sx={{ opacity: 0.85 }}
              aria-label={`Logged in as ${displayName}`}
            >
              {displayName}
            </Typography>
            <Button
              color="inherit"
              variant="outlined"
              size="small"
              onClick={() => auth.signoutRedirect()}
              sx={{ borderColor: 'rgba(255,255,255,0.4)', '&:hover': { borderColor: 'inherit' } }}
            >
              Log Out
            </Button>
          </Stack>
        </Toolbar>
      </AppBar>

      <Box component="main" sx={{ flex: 1, py: 4 }}>
        <Container maxWidth="xl">
          {children}
        </Container>
      </Box>

      <Box
        component="footer"
        sx={{ py: 2, px: 3, backdropFilter: 'blur(10px)', bgcolor: 'rgba(0,0,0,0.2)' }}
      >
        <Stack direction="row" spacing={3} sx={{ justifyContent: 'center', alignItems: 'center' }}>
          <Link
            href="https://aspire.dev"
            target="_blank"
            rel="noopener noreferrer"
            color="text.secondary"
            underline="hover"
            variant="body2"
          >
            Learn more about Aspire
            <Box
              component="span"
              sx={{
                position: 'absolute',
                width: 1,
                height: 1,
                overflow: 'hidden',
                clip: 'rect(0,0,0,0)',
                whiteSpace: 'nowrap',
              }}
            >
              (opens in new tab)
            </Box>
          </Link>
          <Link
            href="https://github.com/dotnet/aspire"
            target="_blank"
            rel="noopener noreferrer"
            aria-label="View Aspire on GitHub (opens in new tab)"
            sx={{ display: 'flex', alignItems: 'center' }}
          >
            <Box
              component="img"
              src="/github.svg"
              alt=""
              aria-hidden="true"
              sx={{ width: 24, height: 24, filter: 'brightness(0) invert(0.6)' }}
            />
          </Link>
        </Stack>
      </Box>
    </Box>
  );
}
