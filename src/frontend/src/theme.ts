import { createTheme } from '@mui/material/styles';

const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#7c92f5',
      dark: '#5b6fd8',
    },
    secondary: {
      main: '#8b5ecf',
      dark: '#6b46a3',
    },
    background: {
      default: '#1a1a2e',
      paper: 'rgba(30, 30, 46, 0.95)',
    },
  },
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    h1: { fontWeight: 700, letterSpacing: '-0.02em' },
    h2: { fontWeight: 600 },
    button: { fontWeight: 600, textTransform: 'none' },
  },
  shape: {
    borderRadius: 8,
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          background: 'linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)',
          backgroundAttachment: 'fixed',
          minHeight: '100vh',
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          backdropFilter: 'blur(10px)',
          border: '1px solid rgba(255, 255, 255, 0.08)',
          boxShadow:
            '0 4px 12px rgba(0, 0, 0, 0.35), 0 1px 3px rgba(0, 0, 0, 0.25)',
          transition: 'transform 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease',
          '&:hover': {
            transform: 'translateY(-4px)',
            borderColor: 'rgba(124, 146, 245, 0.4)',
            boxShadow:
              '0 12px 24px rgba(0, 0, 0, 0.45), 0 4px 8px rgba(124, 146, 245, 0.15)',
          },
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          background: 'rgba(26, 26, 46, 0.85)',
          boxShadow: 'none',
        },
      },
    },
    MuiDrawer: {
      styleOverrides: {
        paper: {
          backgroundImage: 'none',
          background: 'rgba(22, 22, 38, 0.98)',
        },
      },
    },
    MuiListItemButton: {
      styleOverrides: {
        root: {
          '&:hover': {
            backgroundColor: 'rgba(255, 255, 255, 0.06)',
          },
        },
      },
    },
  },
});

export default theme;
