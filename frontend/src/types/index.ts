// Auth
export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface AuthResponse {
  userId: string;
  username: string;
  email: string;
  token: string;
  roles: string[];
}

export interface UserInfo {
  userId: string;
  username: string;
  email: string;
  roles: string[];
}

// Products
export interface CreateProductRequest {
  name: string;
  category?: string;
  description?: string;
  sellingPoints: string[];
  price: number;
  tags: string[];
  generationConfig?: string;
}

export interface UpdateProductRequest {
  name?: string;
  category?: string;
  description?: string;
  sellingPoints?: string[];
  price?: number;
  tags?: string[];
  generationConfig?: string;
}

export interface ProductResponse {
  id: string;
  userId: string;
  name: string;
  category?: string;
  description?: string;
  sellingPoints: string[];
  price: number;
  tags: string[];
  generationConfig?: string;
  createdAt: string;
  updatedAt: string;
}

// Generation
export interface CreateGenerationTaskRequest {
  productId: string;
  imageCount: number;
  textVariantsCount: number;
  imageTemplateIds: string[];
  textTemplateIds: string[];
  useReferenceImage: boolean;
  style: string;
}

export interface GenerationTaskResponse {
  id: string;
  productId: string;
  status: string;
  progress: number;
  statusMessage?: string;
  imageCount: number;
  textVariantsCount: number;
  estimatedCost: number;
  actualCost: number;
  createdAt: string;
}

export interface GeneratedImageResponse {
  id: string;
  taskId: string;
  imageTemplateId: string;
  imageUrl: string;
  status: string;
  isSelected: boolean;
  createdAt: string;
}

export interface GeneratedTextResponse {
  id: string;
  taskId: string;
  copywritingTemplateId: string;
  content: string;
  status: string;
  isSelected: boolean;
  createdAt: string;
}

// Templates
export interface ImageTemplate {
  id: string;
  name: string;
  category?: string;
  description?: string;
  promptTemplate: string;
  style: string;
  thumbnailUrl?: string;
  isBuiltin: boolean;
  usageCount: number;
}

export interface CopywritingTemplate {
  id: string;
  name: string;
  templateType: string;
  content: string;
  variables: string[];
  isBuiltin: boolean;
}

// Statistics
export interface BudgetStatus {
  dailyBudget: number;
  usedBudget: number;
  remainingBudget: number;
  estimatedRemainingTasks: number;
}

export interface CostStats {
  startDate: string;
  endDate: string;
  imageCost: number;
  textCost: number;
  totalCost: number;
  imageCount: number;
  textCount: number;
}

export interface GenerationStats {
  total: number;
  completed: number;
  failed: number;
  period: string;
}

// SignalR
export interface TaskProgressUpdate {
  taskId: string;
  progress: number;
  status: string;
  message: string;
}
