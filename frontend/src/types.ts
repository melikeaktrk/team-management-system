export type AuthResponse = {
  token: string;
  expiresAt: string;
  userName: string;
  email: string;
};

export type AuthMeResponse = {
  userName: string | null;
  email: string;
  roles: string[];
  message: string;
};

export type User = {
  id: string;
  userName: string;
  email: string;
  firstName?: string | null;
  lastName?: string | null;
  isActive: boolean;
  roles?: string[];
};

export type UserListResponse = User;

export type UserCreateRequest = {
  userName: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  role?: string;
};

export type UserUpdateRequest = {
  userName?: string;
  email?: string;
  firstName?: string | null;
  lastName?: string | null;
  role?: string;
  isActive?: boolean;
};

export type UserStatusUpdateRequest = {
  isActive: boolean;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  userName: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
};

export type Project = {
  id: string;
  name: string;
  description: string | null;
  status: string;
  startDate?: string | null;
  dueDate?: string | null;
  createdAt?: string;
};

export type ProjectCreateRequest = {
  name: string;
  description?: string;
  startDate?: string | null;
  dueDate?: string | null;
};

export type ProjectUpdateRequest = {
  name?: string;
  description?: string | null;
  status?: string;
  startDate?: string | null;
  dueDate?: string | null;
};

export type ProjectMemberRequest = {
  userId: string;
  role: string;
};
export type ProjectMember = {
  id: string;
  projectId: string;
  userId: string;
  role: string;
  joinedAt: string;
};

export type TaskItem = {
  id: string;
  projectId: string;
  title: string;
  description?: string | null;
  status: string;
  priority: string;
  dueDate?: string | null;
  assignedToUserId?: string | null;
  createdAt?: string;
};

export type TaskItemCreateRequest = {
  projectId: string;
  title: string;
  description?: string;
  priority?: string;
  dueDate?: string | null;
  assignedToUserId?: string | null;
};

export type TaskItemUpdateRequest = {
  title?: string;
  description?: string | null;
  status?: string;
  priority?: string;
  dueDate?: string | null;
  assignedToUserId?: string | null;
};

export type TaskCommentCreateRequest = {
  taskItemId: string;
  content: string;
};

export type NotificationItem = {
  id: string;
  title: string;
  message: string;
};
