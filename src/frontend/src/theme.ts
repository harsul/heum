import { createTheme } from '@mui/material/styles';

const SHADOW_SM = '0 0 2px 0 rgba(145,158,171,0.20), 0 12px 24px -4px rgba(145,158,171,0.12)';
const SHADOW_MD = '0 0 2px 0 rgba(145,158,171,0.24), 0 16px 32px -4px rgba(145,158,171,0.24)';

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#00AB55',
      light: '#5BE49B',
      dark: '#007B40',
    },
    secondary: {
      main: '#00AB55',
      dark: '#007B40',
    },
    success: {
      main: '#00AB55',
      light: '#5BE49B',
      dark: '#007B40',
    },
    text: {
      primary: '#212B36',
      secondary: '#637381',
    },
    background: {
      default: '#F4F6F8',
      paper: '#FFFFFF',
    },
  },
  typography: {
    fontFamily: "'Public Sans', sans-serif",
    h1: { fontWeight: 700, letterSpacing: '-0.02em' },
    h2: { fontWeight: 700 },
    h4: { fontWeight: 700 },
    h6: { fontWeight: 700 },
    button: { fontWeight: 600, textTransform: 'none' },
    body2: { color: '#637381' },
  },
  shape: {
    borderRadius: 8,
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          background: '#F4F6F8',
          minHeight: '100vh',
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          backgroundColor: '#FFFFFF',
          borderRadius: 16,
          border: 'none',
          boxShadow: SHADOW_SM,
        },
      },
    },
    MuiCardHeader: {
      styleOverrides: {
        title: {
          fontSize: '1rem',
          fontWeight: 600,
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          backgroundColor: 'rgba(255, 255, 255, 0.80)',
          backdropFilter: 'blur(12px)',
          border: 'none',
          boxShadow: SHADOW_SM,
          color: '#212B36',
        },
      },
    },
    MuiDrawer: {
      styleOverrides: {
        paper: {
          backgroundImage: 'none',
          backgroundColor: '#FFFFFF',
          color: '#212B36',
          borderRight: 'none',
          boxShadow: SHADOW_SM,
        },
      },
    },
    MuiListItemButton: {
      styleOverrides: {
        root: {
          transition: 'none',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
        },
      },
    },
    MuiTableRow: {
      styleOverrides: {
        root: {
          '&:last-child .MuiTableCell-root': {
            borderBottom: 0,
          },
        },
      },
    },
    MuiMenuItem: {
      defaultProps: {
        dense: true,
      },
      styleOverrides: {
        root: {
          fontSize: '0.8125rem',
        },
      },
    },
    MuiTextField: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiOutlinedInput: {
      defaultProps: {
        size: 'small',
      },
      styleOverrides: {
        root: {
          borderRadius: 12,
        },
      },
    },
    MuiSelect: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiFormControl: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiButton: {
      defaultProps: {
        size: 'small',
      },
      styleOverrides: {
        root: {
          borderRadius: 12,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 600,
        },
        filled: {
          backgroundColor: 'rgba(145, 158, 171, 0.12)',
          color: '#637381',
          '&.MuiChip-colorSuccess': {
            backgroundColor: 'rgba(0, 171, 85, 0.12)',
            color: '#007B40',
          },
          '&.MuiChip-colorWarning': {
            backgroundColor: 'rgba(255, 171, 0, 0.12)',
            color: '#B76E00',
          },
          '&.MuiChip-colorError': {
            backgroundColor: 'rgba(255, 72, 66, 0.12)',
            color: '#B71D18',
          },
          '&.MuiChip-colorInfo': {
            backgroundColor: 'rgba(24, 144, 255, 0.12)',
            color: '#0958D9',
          },
        },
      },
    },
  },
  shadows: [
    'none',
    '0 1px 2px 0 rgba(145,158,171,0.16)',
    SHADOW_SM,
    SHADOW_SM,
    SHADOW_SM,
    SHADOW_SM,
    SHADOW_SM,
    SHADOW_SM,
    SHADOW_SM,
    SHADOW_SM,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
    SHADOW_MD,
  ],
});

export default theme;
