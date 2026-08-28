import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';

interface ErrorFallbackProps {
  error: Error;
  resetErrorBoundary: () => void;
}

export function ErrorFallback({ error, resetErrorBoundary }: ErrorFallbackProps) {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '100vh',
        textAlign: 'center',
        px: 3,
      }}
    >
      <Typography variant="h1" sx={{ fontSize: 80, fontWeight: 700, color: 'primary.main', mb: 1 }}>
        Oops!
      </Typography>
      <Typography variant="h5" sx={{ fontWeight: 600, mb: 1 }}>
        Something went wrong
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ maxWidth: 480, mb: 3 }}>
        {error.message || 'An unexpected error occurred. Please try again.'}
      </Typography>
      <Button variant="contained" onClick={resetErrorBoundary}>
        Try again
      </Button>
    </Box>
  );
}
