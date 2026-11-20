import { Box, Button, Stack, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';

function ErrorLayout({
  code,
  title,
  message,
}: {
  code: string;
  title: string;
  message: string;
}) {
  const navigate = useNavigate();

  return (
    <Box
      sx={{
        minHeight: '60vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        textAlign: 'center',
        p: 3,
      }}
    >
      <Stack spacing={2} alignItems="center">
        <Typography variant="h2" color="text.secondary" sx={{ fontWeight: 700 }}>
          {code}
        </Typography>
        <Typography variant="h5">{title}</Typography>
        <Typography color="text.secondary" sx={{ maxWidth: 420 }}>
          {message}
        </Typography>
        <Button variant="contained" onClick={() => navigate('/')}>
          На дашборд
        </Button>
      </Stack>
    </Box>
  );
}

export function NotFoundPage() {
  return (
    <ErrorLayout
      code="404"
      title="Страница не найдена"
      message="Запрошенная страница не существует или была перемещена."
    />
  );
}

export function ForbiddenPage() {
  return (
    <ErrorLayout
      code="403"
      title="Доступ запрещён"
      message="У вашей роли нет прав на этот раздел. Если доступ необходим для работы, обратитесь к администратору системы."
    />
  );
}
