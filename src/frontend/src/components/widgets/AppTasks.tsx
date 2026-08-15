import { useState } from 'react';
import Card from '@mui/material/Card';
import CardHeader from '@mui/material/CardHeader';
import Checkbox from '@mui/material/Checkbox';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemText from '@mui/material/ListItemText';

interface AppTasksProps {
  title: string;
  list: { id: string; name: string }[];
}

export function AppTasks({ title, list }: AppTasksProps) {
  const [checked, setChecked] = useState<string[]>([]);

  const handleToggle = (id: string) => {
    setChecked((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id],
    );
  };

  return (
    <Card>
      <CardHeader title={title} />
      <List disablePadding sx={{ px: 2, pb: 2 }}>
        {list.map((task) => (
          <ListItem
            key={task.id}
            disableGutters
            secondaryAction={
              <Checkbox
                edge="end"
                checked={checked.includes(task.id)}
                onChange={() => handleToggle(task.id)}
              />
            }
          >
            <ListItemText
              primary={task.name}
              sx={{
                textDecoration: checked.includes(task.id) ? 'line-through' : 'none',
                color: checked.includes(task.id) ? 'text.disabled' : 'text.primary',
              }}
            />
          </ListItem>
        ))}
      </List>
    </Card>
  );
}
