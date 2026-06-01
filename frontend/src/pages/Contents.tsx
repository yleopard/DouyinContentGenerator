import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Stack, Title, Card, Table, Badge, Text, Group, Progress, Button, Loader, Center,
  ActionIcon, Image, Modal, SimpleGrid, Tooltip, CopyButton, Accordion,
} from '@mantine/core';
import {
  IconDownload, IconEye, IconChevronDown, IconCopy, IconCheck, IconPhoto, IconFileText,
} from '@tabler/icons-react';
import { generationService } from '../services/generationService';
import type { GenerationTaskResponse, GeneratedImageResponse, GeneratedTextResponse } from '../types';

const statusColors: Record<string, string> = { pending: 'gray', processing: 'blue', completed: 'green', failed: 'red', cancelled: 'orange' };
const statusLabels: Record<string, string> = { pending: '等待中', processing: '生成中', completed: '已完成', failed: '失败', cancelled: '已取消' };

function downloadImage(url: string, filename: string) {
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.target = '_blank';
  a.rel = 'noopener';
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
}

function ContentPanel({ taskId }: { taskId: string }) {
  const { data: images = [], isLoading: loadingImg } = useQuery({
    queryKey: ['generated-images', taskId],
    queryFn: () => generationService.getImages(taskId).then(r => r.data),
    enabled: !!taskId,
  });
  const { data: texts = [], isLoading: loadingTxt } = useQuery({
    queryKey: ['generated-texts', taskId],
    queryFn: () => generationService.getTexts(taskId).then(r => r.data),
    enabled: !!taskId,
  });
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  if (loadingImg && loadingTxt) return <Center py="md"><Loader size="sm" /></Center>;

  return (
    <Stack gap="md" p="md">
      {/* Images */}
      {images.length > 0 && (
        <Stack gap="xs">
          <Group gap="xs">
            <IconPhoto size={16} />
            <Text fw={600} size="sm">生成图片 ({images.length})</Text>
          </Group>
          <SimpleGrid cols={{ base: 2, sm: 3, md: 4, lg: 5 }}>
            {images.map((img) => (
              <Card key={img.id} padding="xs" radius="md" withBorder style={{ position: 'relative' }}>
                <Card.Section>
                  <Image src={img.imageUrl} alt="生成图片" height={140} fit="cover"
                    style={{ cursor: img.status === 'success' ? 'pointer' : 'default', opacity: img.status === 'success' ? 1 : 0.4 }}
                    onClick={() => img.status === 'success' && setPreviewUrl(img.imageUrl)} />
                </Card.Section>
                <Group justify="space-between" mt={6}>
                  <Badge size="xs" color={img.status === 'success' ? 'green' : 'red'}>
                    {img.status === 'success' ? '成功' : '失败'}
                  </Badge>
                  {img.status === 'success' && (
                    <Group gap={4}>
                      <Tooltip label="查看大图">
                        <ActionIcon variant="subtle" size="sm" onClick={() => setPreviewUrl(img.imageUrl)}>
                          <IconEye size={14} />
                        </ActionIcon>
                      </Tooltip>
                      <Tooltip label="下载">
                        <ActionIcon variant="subtle" size="sm" color="pink"
                          onClick={() => downloadImage(img.imageUrl, `generated-${img.id.slice(0, 8)}.jpg`)}>
                          <IconDownload size={14} />
                        </ActionIcon>
                      </Tooltip>
                    </Group>
                  )}
                </Group>
              </Card>
            ))}
          </SimpleGrid>
        </Stack>
      )}

      {/* Texts */}
      {texts.length > 0 && (
        <Stack gap="xs">
          <Group gap="xs">
            <IconFileText size={16} />
            <Text fw={600} size="sm">生成文案 ({texts.length})</Text>
          </Group>
          <SimpleGrid cols={{ base: 1, md: 2 }}>
            {texts.map((txt) => (
              <Card key={txt.id} padding="sm" radius="md" withBorder>
                <Text size="sm" style={{ whiteSpace: 'pre-wrap', maxHeight: 120, overflow: 'auto' }}>
                  {txt.content}
                </Text>
                <Group justify="space-between" mt="xs">
                  <Badge size="xs" color={txt.status === 'success' ? 'green' : 'red'}>
                    {txt.status === 'success' ? '成功' : '失败'}
                  </Badge>
                  {txt.status === 'success' && (
                    <CopyButton value={txt.content}>
                      {({ copied, copy }) => (
                        <Tooltip label={copied ? '已复制' : '复制文案'}>
                          <ActionIcon variant="subtle" size="sm" color={copied ? 'green' : 'gray'} onClick={copy}>
                            {copied ? <IconCheck size={14} /> : <IconCopy size={14} />}
                          </ActionIcon>
                        </Tooltip>
                      )}
                    </CopyButton>
                  )}
                </Group>
              </Card>
            ))}
          </SimpleGrid>
        </Stack>
      )}

      {images.length === 0 && texts.length === 0 && !loadingImg && !loadingTxt && (
        <Text c="dimmed" size="sm">该任务暂无生成内容</Text>
      )}

      {/* Image Preview Modal */}
      <Modal opened={!!previewUrl} onClose={() => setPreviewUrl(null)} size="xl" title="图片预览" centered>
        {previewUrl && (
          <Stack>
            <Image src={previewUrl} alt="预览" fit="contain" style={{ maxHeight: '70vh' }} />
            <Button fullWidth leftSection={<IconDownload size={16} />} color="pink"
              onClick={() => downloadImage(previewUrl!, `download-${Date.now()}.jpg`)}>
              下载图片
            </Button>
          </Stack>
        )}
      </Modal>
    </Stack>
  );
}

export function Contents() {
  const { data: tasks = [], isLoading } = useQuery({
    queryKey: ['generation-tasks'],
    queryFn: () => generationService.listTasks().then(r => r.data),
    refetchInterval: 5000,
  });

  return (
    <Stack>
      <Title order={3}>内容库</Title>

      {!isLoading && tasks.length === 0 && (
        <Text ta="center" c="dimmed" py="xl">暂无生成任务，去「内容生成」创建任务吧</Text>
      )}

      <Accordion variant="separated">
        {tasks.map((task) => (
          <Accordion.Item key={task.id} value={task.id}>
            <Accordion.Control>
              <Group justify="space-between" wrap="nowrap">
                <Group gap="sm">
                  <Text size="sm" fw={500} ff="monospace">{task.id.slice(0, 8)}...</Text>
                  <Badge color={statusColors[task.status]} size="sm">
                    {statusLabels[task.status] || task.status}
                  </Badge>
                </Group>
                <Group gap="md" visibleFrom="sm">
                  {task.status === 'processing' && (
                    <Group gap={6}>
                      <Progress value={task.progress} size="xs" w={80} color="pink" />
                      <Text size="xs" c="dimmed">{task.progress}%</Text>
                    </Group>
                  )}
                  <Text size="xs" c="dimmed">¥{task.actualCost.toFixed(2)}</Text>
                  <Text size="xs" c="dimmed">{new Date(task.createdAt).toLocaleDateString()}</Text>
                </Group>
              </Group>
            </Accordion.Control>
            <Accordion.Panel>
              {task.status === 'completed' ? (
                <ContentPanel taskId={task.id} />
              ) : (
                <Text c="dimmed" size="sm" ta="center" py="md">
                  {task.status === 'processing' ? '任务生成中，完成后方可查看内容...' :
                   task.status === 'failed' ? `生成失败: ${task.statusMessage || '未知错误'}` :
                   '任务尚未完成'}
                </Text>
              )}
            </Accordion.Panel>
          </Accordion.Item>
        ))}
      </Accordion>
    </Stack>
  );
}
