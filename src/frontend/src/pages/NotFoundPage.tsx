import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import { useNavigate } from 'react-router-dom';

export function NotFoundPage() {
  const navigate = useNavigate();

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
      <Typography variant="h1" sx={{ fontSize: 120, fontWeight: 700, color: 'primary.main', mb: 1 }}>
        404
      </Typography>
      <Typography variant="h5" sx={{ fontWeight: 600, mb: 1 }}>
        Page not found
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ maxWidth: 480, mb: 3 }}>
        The page you're looking for doesn't exist or has been moved.
      </Typography>
      <Button variant="contained" onClick={() => navigate('/')}>
        Go to home
      </Button>
    </Box>
  );
}
