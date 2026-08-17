import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardHeader from '@mui/material/CardHeader';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

export interface TimelineEvent {
  id: string;
  title: string;
  time: string;
  color?: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
}

interface AppOrderTimelineProps {
  title: string;
  list: TimelineEvent[];
}

export function AppOrderTimeline({ title, list }: AppOrderTimelineProps) {
  return (
    <Card>
      <CardHeader title={title} />
      <Stack spacing={3} sx={{ p: 3 }}>
        {list.map((item, index) => (
          <Stack key={item.id} direction="row" spacing={2}>
            <Stack direction="column" sx={{ alignItems: 'center', flexShrink: 0 }}>
              <Box
                sx={{
                  width: 12,
                  height: 12,
                  borderRadius: '50%',
                  bgcolor: `${item.color ?? 'primary'}.main`,
                }}
              />
              {index !== list.length - 1 ? (
                <Box sx={{ flex: 1, width: '1px', minHeight: 24, bgcolor: 'divider', mt: 0.5 }} />
              ) : null}
            </Stack>
            <Box>
              <Typography variant="subtitle2">{item.title}</Typography>
              <Typography variant="caption" color="text.secondary">
                {item.time}
              </Typography>
            </Box>
          </Stack>
        ))}
      </Stack>
    </Card>
  );
}
