import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Table, Button, Group, Modal, TextInput, Textarea, NumberInput,
  TagsInput, Stack, Title, ActionIcon, Badge, Card, Text,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconPlus, IconEdit, IconTrash, IconUpload } from '@tabler/icons-react';
import { productService } from '../services/productService';
import type { CreateProductRequest, ProductResponse } from '../types';

export function Products() {
  const [opened, { open, close }] = useDisclosure(false);
  const [editing, setEditing] = useState<ProductResponse | null>(null);
  const [form, setForm] = useState<CreateProductRequest>({
    name: '', category: '', description: '', sellingPoints: [],
    price: 0, tags: [], generationConfig: '',
  });
  const queryClient = useQueryClient();

  const { data: products = [], isLoading } = useQuery({
    queryKey: ['products'],
    queryFn: () => productService.list().then(r => r.data),
  });

  const createMutation = useMutation({
    mutationFn: productService.create,
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['products'] }); close(); resetForm(); },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => productService.update(id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['products'] }); close(); resetForm(); },
  });

  const deleteMutation = useMutation({
    mutationFn: productService.delete,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] }),
  });

  const resetForm = () => {
    setForm({ name: '', category: '', description: '', sellingPoints: [], price: 0, tags: [], generationConfig: '' });
    setEditing(null);
  };

  const handleEdit = (p: ProductResponse) => {
    setEditing(p);
    setForm({
      name: p.name, category: p.category ?? '', description: p.description ?? '',
      sellingPoints: p.sellingPoints, price: p.price, tags: p.tags, generationConfig: p.generationConfig ?? '',
    });
    open();
  };

  const handleSubmit = () => {
    if (editing) {
      updateMutation.mutate({ id: editing.id, data: form });
    } else {
      createMutation.mutate(form);
    }
  };

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={3}>产品管理</Title>
        <Button color="pink" leftSection={<IconPlus size={16} />} onClick={() => { resetForm(); open(); }}>
          添加产品
        </Button>
      </Group>

      <Card shadow="sm" radius="md" withBorder>
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>产品名称</Table.Th>
              <Table.Th>分类</Table.Th>
              <Table.Th>价格</Table.Th>
              <Table.Th>标签</Table.Th>
              <Table.Th>操作</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {products.map((p) => (
              <Table.Tr key={p.id}>
                <Table.Td fw={500}>{p.name}</Table.Td>
                <Table.Td>{p.category || '-'}</Table.Td>
                <Table.Td>¥{p.price}</Table.Td>
                <Table.Td>
                  <Group gap={4}>
                    {p.tags.slice(0, 3).map((t) => (
                      <Badge key={t} size="sm" variant="light" color="pink">{t}</Badge>
                    ))}
                  </Group>
                </Table.Td>
                <Table.Td>
                  <Group gap={4}>
                    <ActionIcon variant="light" color="blue" onClick={() => handleEdit(p)}>
                      <IconEdit size={16} />
                    </ActionIcon>
                    <ActionIcon variant="light" color="red" onClick={() => deleteMutation.mutate(p.id)}>
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
        {!isLoading && products.length === 0 && (
          <Text ta="center" c="dimmed" py="xl">暂无产品，点击"添加产品"开始</Text>
        )}
      </Card>

      <Modal opened={opened} onClose={close} title={editing ? '编辑产品' : '添加产品'} size="lg">
        <Stack>
          <TextInput label="产品名称" required value={form.name}
            onChange={(e) => setForm({ ...form, name: e.currentTarget.value })} />
          <TextInput label="分类" value={form.category}
            onChange={(e) => setForm({ ...form, category: e.currentTarget.value })} />
          <Textarea label="描述" value={form.description}
            onChange={(e) => setForm({ ...form, description: e.currentTarget.value })} />
          <NumberInput label="价格" required value={form.price} min={0}
            onChange={(v) => setForm({ ...form, price: Number(v) || 0 })} />
          <TagsInput label="卖点" value={form.sellingPoints}
            onChange={(v) => setForm({ ...form, sellingPoints: v })} />
          <TagsInput label="标签" value={form.tags}
            onChange={(v) => setForm({ ...form, tags: v })} />
          <Button color="pink" onClick={handleSubmit}
            loading={createMutation.isPending || updateMutation.isPending}>
            {editing ? '保存' : '创建'}
          </Button>
        </Stack>
      </Modal>
    </Stack>
  );
}
