import { SimpleGrid, Card, Text, Progress, Stack, Title, Group, ThemeIcon } from '@mantine/core';
import { IconCoins, IconArrowUpRight, IconArrowDownRight } from '@tabler/icons-react';
import { useCostMonitor } from '../hooks/useCostMonitor';

export function Statistics() {
  const { budget, costStats, genStats, isLoading } = useCostMonitor();

  const cards = [
    {
      title: '今日预算',
      value: `¥${budget?.dailyBudget.toFixed(2) ?? '200.00'}`,
      subtitle: `剩余 ¥${budget?.remainingBudget.toFixed(2) ?? '0.00'}`,
      progress: budget ? ((budget.dailyBudget - budget.remainingBudget) / budget.dailyBudget) * 100 : 0,
      color: 'pink',
      icon: IconCoins,
    },
    {
      title: '总成本',
      value: `¥${costStats?.totalCost.toFixed(2) ?? '0.00'}`,
      subtitle: `图片 ¥${costStats?.imageCost.toFixed(2) ?? '0.00'} + 文案 ¥${costStats?.textCost.toFixed(2) ?? '0.00'}`,
      color: 'blue',
      icon: IconArrowUpRight,
    },
    {
      title: '生成调用',
      value: `${(costStats?.imageCount ?? 0) + (costStats?.textCount ?? 0)}`,
      subtitle: `图片 ${costStats?.imageCount ?? 0} 次 + 文案 ${costStats?.textCount ?? 0} 次`,
      color: 'green',
      icon: IconArrowDownRight,
    },
  ];

  return (
    <Stack>
      <Title order={3}>统计分析</Title>

      <SimpleGrid cols={{ base: 1, md: 3 }}>
        {cards.map((card) => (
          <Card key={card.title} shadow="sm" padding="lg" radius="md" withBorder>
            <Group justify="space-between" mb="xs">
              <Text size="xs" c="dimmed">{card.title}</Text>
              <ThemeIcon variant="light" size="md" color={card.color}>
                <card.icon size={16} />
              </ThemeIcon>
            </Group>
            <Text fw={700} size="xl">{isLoading ? '...' : card.value}</Text>
            <Text size="xs" c="dimmed" mt={4}>{card.subtitle}</Text>
            <Progress value={card.progress ?? 0} color={card.color} mt="md" size="sm" radius="xl" />
          </Card>
        ))}
      </SimpleGrid>

      {genStats && (
        <Card shadow="sm" padding="lg" radius="md" withBorder>
          <Title order={5} mb="md">任务统计</Title>
          <SimpleGrid cols={3}>
            <Stack gap={0} align="center">
              <Text size="xl" fw={700}>{genStats.total}</Text>
              <Text size="sm" c="dimmed">总任务数</Text>
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
