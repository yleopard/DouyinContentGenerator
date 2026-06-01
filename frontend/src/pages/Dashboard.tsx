import { SimpleGrid, Card, Text, Group, Progress, Stack, Title } from '@mantine/core';
import { IconPhoto, IconFileText, IconCoin, IconCheck } from '@tabler/icons-react';
import { useCostMonitor } from '../hooks/useCostMonitor';

export function Dashboard() {
  const { budget, costStats, genStats, isLoading } = useCostMonitor();

  const stats = [
    {
      title: '今日预算剩余',
      value: `¥${budget?.remainingBudget.toFixed(2) ?? '0.00'}`,
      icon: IconCoin,
      color: 'pink',
      progress: budget ? (budget.remainingBudget / budget.dailyBudget) * 100 : 0,
    },
    {
      title: '生成图片数',
      value: String(costStats?.imageCount ?? 0),
      icon: IconPhoto,
      color: 'blue',
    },
    {
      title: '生成文案数',
      value: String(costStats?.textCount ?? 0),
      icon: IconFileText,
      color: 'green',
    },
    {
      title: '任务完成率',
      value: genStats ? `${genStats.total > 0 ? Math.round((genStats.completed / genStats.total) * 100) : 0}%` : '0%',
      icon: IconCheck,
      color: 'violet',
    },
  ];

  return (
    <Stack>
      <Title order={3} mb="md">仪表盘</Title>
      <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }}>
        {stats.map((stat) => (
          <Card key={stat.title} shadow="sm" padding="lg" radius="md" withBorder>
            <Group justify="space-between" mb="xs">
              <Text size="xs" c="dimmed">{stat.title}</Text>
              <stat.icon size={20} color={`var(--mantine-color-${stat.color}-6)`} />
            </Group>
            <Text fw={700} size="xl">{isLoading ? '...' : stat.value}</Text>
            {'progress' in stat && (
              <Progress value={stat.progress} color={stat.color} mt="md" size="sm" radius="xl" />
            )}
          </Card>
        ))}
      </SimpleGrid>

      {genStats && (
        <Card shadow="sm" padding="lg" radius="md" withBorder mt="md">
          <Text fw={600} mb="md">本月生成统计</Text>
          <SimpleGrid cols={3}>
            <Stack gap={0} align="center">
              <Text size="xl" fw={700}>{genStats.total}</Text>
              <Text size="sm" c="dimmed">总任务</Text>
            </Stack>
            <Stack gap={0} align="center">
              <Text size="xl" fw={700} c="green">{genStats.completed}</Text>
              <Text size="sm" c="dimmed">已完成</Text>
            </Stack>
            <Stack gap={0} align="center">
              <Text size="xl" fw={700} c="red">{genStats.failed}</Text>
              <Text size="sm" c="dimmed">失败</Text>
            </Stack>
          </SimpleGrid>
        </Card>
      )}
    </Stack>
  );
}
