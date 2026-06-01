import { useState } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
  AppShell, Group, Title, Text, NavLink, ThemeIcon, ActionIcon,
  Burger, Box, useMantineTheme,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import {
  IconDashboard, IconPackage, IconWand, IconPhoto,
  IconSettings, IconChartBar, IconLogout,
} from '@tabler/icons-react';
import { useAuthStore } from '../../store';

const navItems = [
  { label: '仪表盘', icon: IconDashboard, path: '/' },
  { label: '产品管理', icon: IconPackage, path: '/products' },
  { label: '内容生成', icon: IconWand, path: '/generate' },
  { label: '内容库', icon: IconPhoto, path: '/contents' },
  { label: '统计分析', icon: IconChartBar, path: '/statistics' },
  { label: '系统设置', icon: IconSettings, path: '/settings' },
];

export function MainLayout() {
  const [opened, { toggle }] = useDisclosure();
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuthStore();
  const theme = useMantineTheme();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <AppShell
      header={{ height: 56 }}
      navbar={{ width: 240, breakpoint: 'sm', collapsed: { mobile: !opened } }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group>
            <Burger opened={opened} onClick={toggle} hiddenFrom="sm" size="sm" />
            <IconWand size={28} color={theme.colors.pink[6]} />
            <Title order={4}>抖音图文带货AI</Title>
          </Group>
          <Group>
            <Text size="sm" c="dimmed">{user?.username}</Text>
            <ActionIcon variant="subtle" color="gray" onClick={handleLogout}>
              <IconLogout size={18} />
            </ActionIcon>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="xs">
        {navItems.map((item) => (
          <NavLink
            key={item.path}
            label={item.label}
            leftSection={
              <ThemeIcon variant="light" size="sm" color="pink">
                <item.icon size={16} />
              </ThemeIcon>
            }
            active={location.pathname === item.path}
            onClick={() => { navigate(item.path); toggle(); }}
            variant="filled"
            mb={4}
            style={{ borderRadius: 8 }}
          />
        ))}
      </AppShell.Navbar>

      <AppShell.Main>
        <Box mih="calc(100vh - 120px)">
          <Outlet />
        </Box>
      </AppShell.Main>
    </AppShell>
  );
}
