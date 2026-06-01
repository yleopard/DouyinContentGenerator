import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Stack, Title, Card, Select, NumberInput, Switch, Button,
  Group, Text, Progress, MultiSelect, Tabs, Badge, Loader, Alert, Timeline, ThemeIcon,
} from '@mantine/core';
import { IconWand, IconPlus, IconAlertCircle, IconCheck, IconX, IconClock, IconLoader, IconPhoto, IconFileText } from '@tabler/icons-react';
import { productService } from '../services/productService';
import { templateService } from '../services/templateService';
import { generationService } from '../services/generationService';
import { useSignalR, useItemStatus } from '../hooks/useSignalR';
import type { ItemStatus } from '../hooks/useSignalR';
import type { GeneratedImageResponse, GeneratedTextResponse, TaskProgressUpdate } from '../types';

const TIMEOUT_MINUTES = 10;

export function Generate() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [productId, setProductId] = useState<string | null>(null);
  const [imageCount, setImageCount] = useState(2);
  const [textVariantsCount, setTextVariantsCount] = useState(3);
  const [selectedImgTemplates, setSelectedImgTemplates] = useState<string[]>([]);
  const [selectedTxtTemplates, setSelectedTxtTemplates] = useState<string[]>([]);
  const [useReferenceImage, setUseReferenceImage] = useState(false);
  const [activeTaskId, setActiveTaskId] = useState<string | null>(null);
  const [progress, setProgress] = useState(0);
  const [statusMessage, setStatusMessage] = useState('');
  const [selectedImage, setSelectedImage] = useState<string | null>(null);
  const [selectedText, setSelectedText] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [taskStatus, setTaskStatus] = useState<string | null>(null);
  const [items, setItems] = useState<Map<string, ItemStatus>>(new Map());
  const startTimeRef = useRef<number>(0);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const { data: products = [], isLoading: productsLoading } = useQuery({
    queryKey: ['products'],
    queryFn: () => productService.list().then(r => r.data),
  });

  const { data: imgTemplates = [] } = useQuery({
    queryKey: ['image-templates'],
    queryFn: () => templateService.getImageTemplates().then(r => r.data),
  });

  const { data: txtTemplates = [] } = useQuery({
    queryKey: ['copywriting-templates'],
    queryFn: () => templateService.getCopywritingTemplates().then(r => r.data),
  });

  const { data: images = [] } = useQuery({
    queryKey: ['generated-images', activeTaskId],
    queryFn: () => generationService.getImages(activeTaskId!).then(r => r.data),
    enabled: !!activeTaskId,
    refetchInterval: 3000,
  });

  const { data: texts = [] } = useQuery({
    queryKey: ['generated-texts', activeTaskId],
    queryFn: () => generationService.getTexts(activeTaskId!).then(r => r.data),
    enabled: !!activeTaskId,
    refetchInterval: 3000,
  });

  // SignalR: overall progress
  useSignalR(activeTaskId ?? undefined, (data: TaskProgressUpdate) => {
    setProgress(data.progress);
    setStatusMessage(data.message);
    if (data.status === 'completed') finishGeneration('completed');
    else if (data.status === 'failed') finishGeneration('failed', data.message);
    else if (data.status === 'cancelled') finishGeneration('cancelled', '任务已取消');
  });

  // SignalR: per-item status
  useItemStatus(activeTaskId ?? undefined, (data: ItemStatus) => {
    setItems(prev => {
      const next = new Map(prev);
      next.set(`${data.type}:${data.name}`, data);
      return next;
    });
  });

  // Poll fallback
  useEffect(() => {
    if (!activeTaskId || !generating) return;
    timerRef.current = setInterval(async () => {
      try {
        const res = await generationService.listTasks();
        const task = res.data.find(t => t.id === activeTaskId);
        if (!task) return;
        setTaskStatus(task.status);
        setProgress(task.progress);
        const elapsed = (Date.now() - startTimeRef.current) / 1000 / 60;
        if (elapsed > TIMEOUT_MINUTES) {
          finishGeneration('timeout', `超时（超过${TIMEOUT_MINUTES}分钟）`);
          return;
        }
        if (task.status === 'completed') finishGeneration('completed');
        else if (task.status === 'failed') finishGeneration('failed', task.statusMessage || '任务失败');
        else if (task.status === 'cancelled') finishGeneration('cancelled', '已取消');
      } catch { /* ignore */ }
    }, 5000);
    return () => { if (timerRef.current) clearInterval(timerRef.current); };
  }, [activeTaskId, generating]);

  const finishGeneration = (status: string, message?: string) => {
    setGenerating(false);
    setTaskStatus(status);
    if (status !== 'completed') setError(message || `生成${status === 'timeout' ? '超时' : '失败'}`);
    if (timerRef.current) clearInterval(timerRef.current);
    queryClient.invalidateQueries({ queryKey: ['generated-images', activeTaskId] });
    queryClient.invalidateQueries({ queryKey: ['generated-texts', activeTaskId] });
  };

  const handleGenerate = () => {
    if (!productId || selectedImgTemplates.length === 0) return;
    setError(null); setTaskStatus(null); setProgress(0); setActiveTaskId(null);
    setGenerating(true); setItems(new Map());
    setStatusMessage('提交任务...');
    startTimeRef.current = Date.now();

    generationService.createTask({
      productId, imageCount, textVariantsCount,
      imageTemplateIds: selectedImgTemplates,
      textTemplateIds: selectedTxtTemplates,
      useReferenceImage, style: 'realistic',
    }).then(res => {
      setActiveTaskId(res.data.id);
      setStatusMessage('正在生成...');
    }).catch(err => {
      setError(err.response?.data?.error || err.message || '创建任务失败');
      setGenerating(false);
    });
  };

  const handleCancel = async () => {
    if (!activeTaskId) return;
    try { await generationService.cancelTask(activeTaskId); finishGeneration('cancelled', '已取消'); } catch { /* */ }
  };

  const groupedImages = images.reduce<Record<string, GeneratedImageResponse[]>>((acc, img) => {
    (acc[img.imageTemplateId] ||= []).push(img); return acc;
  }, {});
  const groupedTexts = texts.reduce<Record<string, GeneratedTextResponse[]>>((acc, txt) => {
    (acc[txt.copywritingTemplateId] ||= []).push(txt); return acc;
  }, {});
  const templateNames = Object.fromEntries(imgTemplates.map(t => [t.id, t.name]));

  // Timeline items
  const itemList = Array.from(items.values()).sort((a, b) => new Date(b.time).getTime() - new Date(a.time).getTime());
  const statusIcon = (s: string) => {
    switch (s) {
      case 'calling': return <ThemeIcon color="blue" size={20} radius="xl"><IconLoader size={12} /></ThemeIcon>;
      case 'success': return <ThemeIcon color="green" size={20} radius="xl"><IconCheck size={12} /></ThemeIcon>;
      case 'failed': return <ThemeIcon color="red" size={20} radius="xl"><IconX size={12} /></ThemeIcon>;
      case 'cancelled': return <ThemeIcon color="gray" size={20} radius="xl"><IconX size={12} /></ThemeIcon>;
      default: return <ThemeIcon color="gray" size={20} radius="xl"><IconClock size={12} /></ThemeIcon>;
    }
  };

  return (
    <Stack>
      <Title order={3}>内容生成</Title>

      {productsLoading ? <Loader mx="auto" mt="xl" /> : products.length === 0 ? (
        <Alert color="yellow" icon={<IconAlertCircle size={16} />}>
          暂无产品，请先
          <Button variant="subtle" size="compact-sm" color="pink" onClick={() => navigate('/products')} ml={4}>
            <IconPlus size={14} /> 添加产品
          </Button>
        </Alert>
      ) : (
        <Card shadow="sm" radius="md" withBorder>
          <Stack>
            <Select label="选择产品" placeholder="搜索或选择产品"
              data={products.map(p => ({ value: p.id, label: `${p.name} (¥${p.price})` }))}
              value={productId} onChange={setProductId} searchable clearable required disabled={generating} />

            <MultiSelect label="图片模板" placeholder="选择图片场景模板" searchable clearable
              data={imgTemplates.map(t => ({ value: t.id, label: `${t.name} [${t.category}]` }))}
              value={selectedImgTemplates} onChange={setSelectedImgTemplates} disabled={generating} />

            <Group grow>
              <NumberInput label="每模板图片数" value={imageCount} onChange={v => setImageCount(Number(v) || 1)} min={1} max={10} disabled={generating} />
              <NumberInput label="每模板文案变体数" value={textVariantsCount} onChange={v => setTextVariantsCount(Number(v) || 1)} min={1} max={10} disabled={generating} />
            </Group>

            <MultiSelect label="文案模板（可选）" placeholder="选择文案模板" searchable clearable
              data={txtTemplates.map(t => ({ value: t.id, label: t.name }))}
              value={selectedTxtTemplates} onChange={setSelectedTxtTemplates} disabled={generating} />

            <Switch label="使用参考图生成（图生图模式）" checked={useReferenceImage}
              onChange={e => setUseReferenceImage(e.currentTarget.checked)} disabled={generating} />

            <Group>
              <Button color="pink" leftSection={<IconWand size={16} />}
                onClick={handleGenerate} loading={generating && !error}
                disabled={!productId || generating}>
                {generating && !error ? '生成中...' : '开始生成'}
              </Button>
              {generating && !error && <Button variant="outline" color="red" onClick={handleCancel}>取消任务</Button>}
            </Group>
          </Stack>
        </Card>
      )}

      {error && (
        <Alert color="red" icon={<IconX size={16} />} title="生成失败" withCloseButton onClose={() => setError(null)}>
          <Text>{error}</Text>
          <Text size="xs" mt="xs" c="dimmed">可能原因：AI Key 未配置或失效、模型名称错误、网络问题或预算不足。</Text>
        </Alert>
      )}

      {/* Progress + Detail Timeline */}
      {(generating || itemList.length > 0) && (
        <Card shadow="sm" radius="md" withBorder>
          <Stack>
            <Group justify="space-between">
              <Text fw={600}>生成进度 ({progress}%)</Text>
              <Badge color={taskStatus === 'completed' ? 'green' : taskStatus === 'failed' ? 'red' : 'blue'}>
                {taskStatus || '等待中'}
              </Badge>
            </Group>
            <Progress value={progress} color={progress < 100 ? 'pink' : 'green'} size="md" radius="xl"
              striped={progress < 100} animated={progress < 100} />
            <Text size="sm" c="dimmed">{statusMessage}</Text>

            {itemList.length > 0 && (
              <>
                <Text size="sm" fw={500} mt="sm">API 调用明细</Text>
                <Timeline active={itemList.filter(i => i.status === 'calling').length} bulletSize={22} lineWidth={2}>
                  {itemList.map((item, idx) => (
                    <Timeline.Item key={idx} bullet={statusIcon(item.status)}
                      title={
                        <Group gap={6}>
                          {item.type === 'image' ? <IconPhoto size={14} /> : <IconFileText size={14} />}
                          <Text size="sm" fw={500}>{item.name}</Text>
                          <Badge size="xs" color={
                            item.status === 'success' ? 'green' : item.status === 'failed' ? 'red' :
                            item.status === 'calling' ? 'blue' : 'gray'
                          }>
                            {item.status === 'calling' ? '调用中' : item.status === 'success' ? '成功' : item.status === 'failed' ? '失败' : item.status}
                          </Badge>
                        </Group>
                      }>
                      <Text size="xs" c="dimmed">{item.detail}</Text>
                      <Text size="xs" c="gray">{new Date(item.time).toLocaleTimeString()}</Text>
                    </Timeline.Item>
                  ))}
                </Timeline>
              </>
            )}
          </Stack>
        </Card>
      )}

      {/* Results */}
      {Object.keys(groupedImages).length > 0 && (
        <Card shadow="sm" radius="md" withBorder>
          <Title order={5} mb="md">生成结果</Title>
          <Tabs defaultValue={Object.keys(groupedImages)[0]}>
            <Tabs.List>
              {Object.keys(groupedImages).map(tplId => (
                <Tabs.Tab key={tplId} value={tplId}>{templateNames[tplId] || tplId}</Tabs.Tab>
              ))}
            </Tabs.List>
            {Object.entries(groupedImages).map(([tplId, imgs]) => (
              <Tabs.Panel key={tplId} value={tplId} pt="md">
                <Group align="flex-start">
                  <Stack style={{ flex: 1 }}>
                    {imgs.map(img => (
                      <Card key={img.id} shadow="xs" padding="sm" withBorder
                        style={{ cursor: 'pointer', border: selectedImage === img.id ? '2px solid #E64980' : undefined }}
                        onClick={() => setSelectedImage(img.id)}>
                        {img.status === 'failed' ? (
                          <Text size="xs" c="red">失败: {img.errorMessage || '未知错误'}</Text>
                        ) : (
                          <Text size="xs" c="dimmed" truncate>{img.imageUrl || '生成中...'}</Text>
                        )}
                        {img.isSelected && <Badge color="pink" size="xs">已选</Badge>}
                      </Card>
                    ))}
                  </Stack>
                  <Stack style={{ flex: 1 }}>
                    {(groupedTexts[tplId] || []).map(txt => (
                      <Card key={txt.id} shadow="xs" padding="sm" withBorder
                        style={{ cursor: 'pointer', border: selectedText === txt.id ? '2px solid #E64980' : undefined }}
                        onClick={() => setSelectedText(txt.id)}>
                        {txt.status === 'failed' ? (
                          <Text size="xs" c="red">失败: {txt.errorMessage || '未知错误'}</Text>
                        ) : (
                          <Text size="sm" lineClamp={4}>{txt.content || '生成中...'}</Text>
                        )}
                        {txt.isSelected && <Badge color="pink" size="xs">已选</Badge>}
                      </Card>
                    ))}
                  </Stack>
                </Group>
              </Tabs.Panel>
            ))}
          </Tabs>
        </Card>
      )}
    </Stack>
  );
}
