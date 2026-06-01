import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import {
  Container, Paper, Title, TextInput, PasswordInput,
  Button, Text, Stack, Alert,
} from '@mantine/core';
import { IconLock } from '@tabler/icons-react';
import { authService } from '../services/authService';
import { useAuthStore } from '../store';

export function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { setAuth } = useAuthStore();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await authService.login({ username, password });
      setAuth(res.data, res.data.token);
      navigate('/');
    } catch (err: any) {
      setError(err.response?.data?.error || '登录失败，请检查用户名和密码');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container size={420} my={80}>
      <Paper withBorder shadow="md" p={30} radius="md">
        <Stack align="center" mb="md">
          <IconLock size={40} color="#E64980" />
          <Title order={3}>登录</Title>
        </Stack>

        {error && <Alert color="red" mb="md">{error}</Alert>}

        <form onSubmit={handleSubmit}>
          <Stack>
            <TextInput
              label="用户名"
              placeholder="请输入用户名"
              value={username}
              onChange={(e) => setUsername(e.currentTarget.value)}
              required
            />
            <PasswordInput
              label="密码"
              placeholder="请输入密码"
              value={password}
              onChange={(e) => setPassword(e.currentTarget.value)}
              required
            />
            <Button type="submit" color="pink" fullWidth loading={loading}>
              登录
            </Button>
          </Stack>
        </form>

        <Text c="dimmed" size="sm" ta="center" mt="md">
          还没有账号？<Link to="/register" style={{ color: '#E64980' }}>立即注册</Link>
        </Text>
      </Paper>
    </Container>
  );
}
