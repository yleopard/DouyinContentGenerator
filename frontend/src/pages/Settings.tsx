import { useState, useEffect } from 'react';
import {
  Stack, Title, Card, TextInput, PasswordInput, Button, Select, Alert, Group, Badge, Text, Loader,
} from '@mantine/core';
import { IconCheck, IconAlertTriangle, IconPlugConnected, IconX, IconDeviceFloppy } from '@tabler/icons-react';
import api from '../services/api';

interface TestResult {
  testing: boolean;
  result?: { success: boolean; message: string; durationSeconds?: number; sample?: string; sampleType?: string; cost?: number; endpoint?: string };
}

// ===== Provider → Models mapping (label = value, supports dynamic addition) =====
type ModelOption = { value: string; label: string };
type ProviderDef = { value: string; label: string; models: ModelOption[] };

const IMAGE_PROVIDERS: ProviderDef[] = [
  {
    value: 'tongyi_wanxiang', label: '通义万相（阿里云）',
    models: ['wan2.1-t2i-turbo', 'wan2.1-t2i-plus', 'wan2.6-t2i'].map(v => ({ value: v, label: v })),
  },
  {
    value: 'glm_cogview', label: 'CogView（智谱AI）',
    models: ['glm-image', 'cogview-4', 'cogview-3-flash'].map(v => ({ value: v, label: v })),
  },
  {
    value: 'baidu_yige', label: '文心一格（百度）',
    models: ['sd_xl', 'ernie-vilg-v2'].map(v => ({ value: v, label: v })),
  },
  {
    value: 'bytedance_seedance', label: '即梦/Seedance（字节）',
    models: ['doubao-seedream-4-0-250828'].map(v => ({ value: v, label: v })),
  },
  {
    value: 'xiaomi_mimo', label: 'MiMo（小米）⚠️ 仅文本',
    models: ['mimo-v2.5-pro'].map(v => ({ value: v, label: v })),
  },
];

const TEXT_PROVIDERS: ProviderDef[] = [
  {
    value: 'tongyi_qianwen', label: '通义千问（阿里云）',
    models: ['qwen-turbo', 'qwen-plus', 'qwen-max'].map(v => ({ value: v, label: v })),
  },
  {
    value: 'glm', label: 'GLM-4（智谱AI）',
    models: ['glm-4-flash', 'glm-4-plus', 'glm-4-air'].map(v => ({ value: v, label: v })),
  },
  {
    value: 'baidu_yiyan', label: '文心一言（百度）',
    models: ['ernie-4.0-turbo'].map(v => ({ value: v, label: v })),
  },
  {
    value: 'bytedance_doubao', label: '豆包（字节）',
    models: ['doubao-pro-32k', 'doubao-lite-32k'].map(v => ({ value: v, label: v })),
  },
  {
    value: 'xiaomi_mimo_text', label: 'MiMo（小米）',
    models: ['mimo-v2.5-pro', 'mimo-v2.5-flash'].map(v => ({ value: v, label: v })),
  },
];

// ===== Backend provider type mapping for test API =====
const PROVIDER_TYPE_MAP: Record<string, string> = {
  tongyi_wanxiang: 'tongyi_wanxiang', glm_cogview: 'glm_cogview',
  baidu_yige: 'baidu_yige', bytedance_seedance: 'bytedance_seedance', xiaomi_mimo: 'xiaomi_mimo_image',
  tongyi_qianwen: 'tongyi_qianwen', glm: 'glm',
  baidu_yiyan: 'baidu_yiyan', bytedance_doubao: 'bytedance_doubao', xiaomi_mimo_text: 'xiaomi_mimo_text',
};

function getProviderModels(provider: string, type: 'image' | 'text') {
  const list = type === 'image' ? IMAGE_PROVIDERS : TEXT_PROVIDERS;
  return list.find(p => p.value === provider)?.models ?? [];
}

const EMPTY_SETTINGS = {
  imageProvider: 'tongyi_wanxiang', imageApiKey: '', imageModel: 'wan2.1-t2i-turbo',
  textProvider: 'tongyi_qianwen', textApiKey: '', textModel: 'qwen-turbo',
  dailyBudget: '200', alertThreshold: '80',
};

export function Settings() {
  const [saved, setSaved] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [imgProvider, setImgProvider] = useState(EMPTY_SETTINGS.imageProvider);
  const [imgApiKey, setImgApiKey] = useState(EMPTY_SETTINGS.imageApiKey);
  const [imgModel, setImgModel] = useState(EMPTY_SETTINGS.imageModel);
  const [txtProvider, setTxtProvider] = useState(EMPTY_SETTINGS.textProvider);
  const [txtApiKey, setTxtApiKey] = useState(EMPTY_SETTINGS.textApiKey);
  const [txtModel, setTxtModel] = useState(EMPTY_SETTINGS.textModel);
  const [dailyBudget, setDailyBudget] = useState(EMPTY_SETTINGS.dailyBudget);
  const [alertThreshold, setAlertThreshold] = useState(EMPTY_SETTINGS.alertThreshold);

  const [imgTest, setImgTest] = useState<TestResult>({ testing: false });
  const [txtTest, setTxtTest] = useState<TestResult>({ testing: false });

  // Dynamic model lists (can grow when user enters a custom model that passes test)
  const [imgModels, setImgModels] = useState<ModelOption[]>([]);
  const [txtModels, setTxtModels] = useState<ModelOption[]>([]);

  // Update model list when provider changes
  useEffect(() => {
    setImgModels(getProviderModels(imgProvider, 'image'));
  }, [imgProvider]);
  useEffect(() => {
    setTxtModels(getProviderModels(txtProvider, 'text'));
  }, [txtProvider]);

  // Add model to list if test succeeded and model isn't already in the list
  const addModelIfNew = (models: ModelOption[], model: string, setModels: (m: ModelOption[]) => void) => {
    if (model && !models.find(m => m.value === model)) {
      setModels([...models, { value: model, label: model }]);
    }
  };

  // Load saved settings on mount
  useEffect(() => {
    (async () => {
      try {
        const res = await api.get('/ai-providers/settings');
        const cfg = res.data?.configJson;
        if (cfg && cfg !== '{}') {
          const s = typeof cfg === 'string' ? JSON.parse(cfg) : cfg;
          if (s.imageProvider) setImgProvider(s.imageProvider);
          if (s.imageApiKey) setImgApiKey(s.imageApiKey);
          if (s.imageModel) setImgModel(s.imageModel);
          if (s.textProvider) setTxtProvider(s.textProvider);
          if (s.textApiKey) setTxtApiKey(s.textApiKey);
          if (s.textModel) setTxtModel(s.textModel);
          if (s.dailyBudget) setDailyBudget(s.dailyBudget);
          if (s.alertThreshold) setAlertThreshold(s.alertThreshold);
        }
      } catch { /* use defaults */ }
      finally { setLoading(false); }
    })();
  }, []);

  // Auto-select first model when provider changes
  const handleImgProviderChange = (v: string | null) => {
    if (!v) return;
    setImgProvider(v);
    setImgTest({ testing: false });
    const models = getProviderModels(v, 'image');
    if (models.length > 0 && !models.find(m => m.value === imgModel)) {
      setImgModel(models[0].value);
    }
  };

  const handleTxtProviderChange = (v: string | null) => {
    if (!v) return;
    setTxtProvider(v);
    setTxtTest({ testing: false });
    const models = getProviderModels(v, 'text');
    if (models.length > 0 && !models.find(m => m.value === txtModel)) {
      setTxtModel(models[0].value);
    }
  };

  const handleTest = async (type: string, apiKey: string, model: string, setResult: (r: TestResult) => void, modelType: 'image' | 'text') => {
    if (!apiKey) return;
    setResult({ testing: true });
    try {
      const res = await api.post('/ai-providers/test', { type, apiKey, model });
      const data = res.data;
      setResult({ testing: false, result: data });
      // Auto-add successfully tested model to the list
      if (data.success && model) {
        if (modelType === 'image') addModelIfNew(imgModels, model, setImgModels);
        else addModelIfNew(txtModels, model, setTxtModels);
      }
    } catch (err: any) {
      // Show detailed error from backend or HTTP error
      const msg = err.response?.data?.message || err.response?.data?.error || err.message || '网络错误';
      setResult({ testing: false, result: { success: false, message: `请求失败: ${msg}` } });
    }
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      const config = { imageProvider: imgProvider, imageApiKey: imgApiKey, imageModel: imgModel,
        textProvider: txtProvider, textApiKey: txtApiKey, textModel: txtModel, dailyBudget, alertThreshold };
      await api.put('/ai-providers/settings', { configJson: JSON.stringify(config) });
      setSaved('success');
    } catch { setSaved('error'); }
    finally { setSaving(false); setTimeout(() => setSaved(null), 3000); }
  };

  if (loading) {
    return <Stack align="center" mt="xl"><Loader size="lg" /><Text c="dimmed">加载设置...</Text></Stack>;
  }

  return (
    <Stack>
      <Title order={3}>系统设置</Title>
      {saved === 'success' && <Alert color="green" icon={<IconCheck size={16} />}>设置已保存到数据库，下次打开将自动加载</Alert>}
      {saved === 'error' && <Alert color="red" icon={<IconX size={16} />}>保存失败，请重试</Alert>}

      {/* Image Provider Card */}
      <Card shadow="sm" radius="md" withBorder>
        <Title order={5} mb="md">图片生成配置</Title>
        <Stack>
          <Select label="提供商" value={imgProvider} onChange={handleImgProviderChange}
            data={IMAGE_PROVIDERS.map(p => ({ value: p.value, label: p.label }))} />
          <Select label="模型" value={imgModel} onChange={v => v && setImgModel(v)}
            data={imgModels} searchable creatable
            getCreateLabel={(query) => `+ 添加 "${query}"`}
            onCreate={(query) => { const m = { value: query, label: query }; setImgModels(c => [...c, m]); return query; }} />
          <PasswordInput label="API Key" value={imgApiKey} onChange={e => setImgApiKey(e.currentTarget.value)} placeholder="sk-xxx" />
          <Group>
            <Button variant="light" color="blue" loading={imgTest.testing}
              leftSection={<IconPlugConnected size={16} />}
              onClick={() => handleTest(PROVIDER_TYPE_MAP[imgProvider], imgApiKey, imgModel, setImgTest, 'image')}
              disabled={!imgApiKey}>测试连接</Button>
            {imgTest.result && (
              <Stack gap="xs" style={{ maxWidth: '100%' }}>
                {imgTest.result.success ? (
                  <Badge size="lg" color="green" leftSection={<IconCheck size={14} />}>
                    {imgTest.result.message} {imgTest.result.cost != null ? ` ¥${imgTest.result.cost}` : ''}
                  </Badge>
                ) : (
                  <Card bg="red.0" p="sm" radius="sm" style={{
                    borderLeft: '3px solid var(--mantine-color-red-5)',
                    overflow: 'visible', maxWidth: '100%',
                  }}>
                    <div style={{
                      whiteSpace: 'pre-wrap', wordBreak: 'break-word',
                      fontSize: 13, color: 'var(--mantine-color-red-9)',
                      lineHeight: 1.5, fontFamily: 'monospace',
                    }}>
                      {imgTest.result.message}
                    </div>
                  </Card>
                )}
                {imgTest.result.endpoint && <Text size="xs" c="dimmed">端点: {imgTest.result.endpoint}</Text>}
                {imgTest.result.success && imgTest.result.sample && imgTest.result.sampleType === 'image' && (
                  <img src={imgTest.result.sample} alt="测试图片" style={{ maxWidth: 200, borderRadius: 8, border: '1px solid #ddd' }} />
                )}
              </Stack>
            )}
          </Group>
        </Stack>
      </Card>

      {/* Text Provider Card */}
      <Card shadow="sm" radius="md" withBorder>
        <Title order={5} mb="md">文案生成配置</Title>
        <Stack>
          <Select label="提供商" value={txtProvider} onChange={handleTxtProviderChange}
            data={TEXT_PROVIDERS.map(p => ({ value: p.value, label: p.label }))} />
          <Select label="模型" value={txtModel} onChange={v => v && setTxtModel(v)}
            data={txtModels} searchable creatable
            getCreateLabel={(query) => `+ 添加 "${query}"`}
            onCreate={(query) => { const m = { value: query, label: query }; setTxtModels(c => [...c, m]); return query; }} />
          <PasswordInput label="API Key" value={txtApiKey} onChange={e => setTxtApiKey(e.currentTarget.value)} placeholder="sk-xxx" />
          <Group>
            <Button variant="light" color="blue" loading={txtTest.testing}
              leftSection={<IconPlugConnected size={16} />}
              onClick={() => handleTest(PROVIDER_TYPE_MAP[txtProvider], txtApiKey, txtModel, setTxtTest, 'text')}
              disabled={!txtApiKey}>测试连接</Button>
            {txtTest.result && (
              <Stack gap="xs" style={{ maxWidth: '100%' }}>
                {txtTest.result.success ? (
                  <Badge size="lg" color="green" leftSection={<IconCheck size={14} />}>
                    {txtTest.result.message} {txtTest.result.cost != null ? ` ¥${txtTest.result.cost}` : ''}
                  </Badge>
                ) : (
                  <Card bg="red.0" p="sm" radius="sm" style={{
                    borderLeft: '3px solid var(--mantine-color-red-5)',
                    overflow: 'visible', maxWidth: '100%',
                  }}>
                    <div style={{
                      whiteSpace: 'pre-wrap', wordBreak: 'break-word',
                      fontSize: 13, color: 'var(--mantine-color-red-9)',
                      lineHeight: 1.5, fontFamily: 'monospace',
                    }}>
                      {txtTest.result.message}
                    </div>
                  </Card>
                )}
                {txtTest.result.endpoint && <Text size="xs" c="dimmed">端点: {txtTest.result.endpoint}</Text>}
                {txtTest.result.success && txtTest.result.sample && txtTest.result.sampleType === 'text' && (
                  <Card bg="gray.0" p="sm" radius="sm" style={{ borderLeft: '3px solid var(--mantine-color-green-5)' }}>
                    <Text size="sm" fs="italic" c="dark.7">&ldquo;{txtTest.result.sample}&rdquo;</Text>
                  </Card>
                )}
              </Stack>
            )}
          </Group>
        </Stack>
      </Card>

      {/* Cost Control Card */}
      <Card shadow="sm" radius="md" withBorder>
        <Title order={5} mb="md">成本控制</Title>
        <Stack>
          <TextInput label="每日预算 (元)" type="number" value={dailyBudget} onChange={e => setDailyBudget(e.currentTarget.value)} />
          <TextInput label="告警阈值 (%)" type="number" value={alertThreshold} onChange={e => setAlertThreshold(e.currentTarget.value)} />
        </Stack>
      </Card>

      <Alert color="yellow" icon={<IconAlertTriangle size={16} />}>
        填写 API Key 后先点击「测试连接」确认可用，切换提供商会自动选择对应模型。
      </Alert>

      <Button color="pink" onClick={handleSave} loading={saving} leftSection={<IconDeviceFloppy size={18} />}>保存设置</Button>
    </Stack>
  );
}
