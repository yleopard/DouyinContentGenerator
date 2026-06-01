import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import {
  Container, Paper, Title, TextInput, PasswordInput,
  Button, Stack, Alert,
} from '@mantine/core';
import { authService } from '../services/authService';
import { useAuthStore } from '../store';

export function Register() {
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
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
      const res = await authService.register({ username, email, password });
      setAuth(res.data, res.data.token);
      navigate('/');
    } catch (err: any) {
      setError(err.response?.data?.error || '注册失败');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container size={420} my={80}>
      <Paper withBorder shadow="md" p={30} radius="md">
        <Title order={3} ta="center" mb="md">注册</Title>

        {error && <Alert color="red" mb="md">{error}</Alert>}

        <form onSubmit={handleSubmit}>
          <Stack>
            <TextInput label="用户名" placeholder="请输入用户名" value={username}
              onChange={(e) => setUsername(e.currentTarget.value)} required />
            <TextInput label="邮箱" placeholder="请输入邮箱" type="email" value={email}
              onChange={(e) => setEmail(e.currentTarget.value)} required />
            <PasswordInput label="密码" placeholder="请输入密码（至少6位）" value={password}
              onChange={(e) => setPassword(e.currentTarget.value)} required minLength={6} />
            <Button type="submit" color="pink" fullWidth loading={loading}>注册</Button>
          </Stack>
        </form>

        <Link to="/login" style={{ color: '#E64980', display: 'block', textAlign: 'center', marginTop: 16 }}>
          已有账号？去登录
        </Link>
      </Paper>
    </Container>
  );
}
