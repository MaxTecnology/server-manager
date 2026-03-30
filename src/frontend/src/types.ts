export type UserSummary = {
  id: string;
  username: string;
  displayName: string;
  roles: string[];
};

export type LoginResponse = {
  accessToken: string;
  expiresAtUtc: string;
  user: UserSummary;
};

export type DashboardMetrics = {
  activeSessions: number;
  disconnectedSessions: number;
  actionsToday: number;
  errorsToday: number;
  generatedAtUtc: string;
};

export type SessionInfo = {
  sessionId: number;
  username: string;
  sessionName: string;
  state: string;
  idleTime: string;
  logonTime: string;
  serverName: string;
};

export type AuditLog = {
  id: string;
  timestampUtc: string;
  operatorUsername: string;
  action: string;
  serverName: string;
  sessionId: number | null;
  targetUsername: string | null;
  processName: string | null;
  success: boolean;
  errorMessage: string | null;
  clientIpAddress: string | null;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type SettingItem = {
  key: string;
  value: string;
  description: string;
};

export type UserItem = {
  id: string;
  username: string;
  displayName: string;
  isActive: boolean;
  roles: string[];
};

export type AllowedProcess = {
  id: string;
  processName: string;
  isActive: boolean;
  createdBy: string;
};

export type ServerItem = {
  id: string;
  name: string;
  hostname: string;
  isDefault: boolean;
  isActive: boolean;
  supportsRds: boolean;
  supportsAd: boolean;
  agentId: string | null;
  agentVersion: string | null;
  agentLastHeartbeatUtc: string | null;
  agentSessionSnapshotUtc: string | null;
  isAgentOnline: boolean;
  hasRecentSnapshot: boolean;
};

export type AgentCommand = {
  id: string;
  serverId: string;
  serverName: string;
  hostname: string;
  requestedBy: string;
  commandText: string;
  status: string;
  requestedAtUtc: string;
  pickedAtUtc: string | null;
  completedAtUtc: string | null;
  assignedAgentId: string | null;
  resultOutput: string | null;
  errorMessage: string | null;
};

export type CreateAdUserRequest = {
  username: string;
  displayName: string;
  password: string;
  userPrincipalName?: string;
  organizationalUnitPath?: string;
  changePasswordAtLogon: boolean;
};

export type ResetAdUserPasswordRequest = {
  password: string;
  changePasswordAtLogon: boolean;
  enableAccount: boolean;
};

export type AdOrganizationalUnit = {
  name: string;
  distinguishedName: string;
  canonicalName: string;
  depth: number;
};
