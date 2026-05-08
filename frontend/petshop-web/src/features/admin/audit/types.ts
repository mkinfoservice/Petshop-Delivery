export interface OperationalAuditFilters {
  page?: number;
  pageSize?: number;
  action?: string;
  targetType?: string;
  targetId?: string;
  correlationId?: string;
  from?: string;
  to?: string;
}

export interface OperationalAuditListItem {
  id: string;
  action: string;
  targetType: string;
  targetId: string;
  targetName: string | null;
  actorUsername: string;
  actorRole: string;
  correlationId: string | null;
  createdAtUtc: string;
  hasPayload: boolean;
}

export interface OperationalAuditListResponse {
  page: number;
  pageSize: number;
  total: number;
  items: OperationalAuditListItem[];
}

export interface OperationalAuditDetailResponse {
  id: string;
  companyId: string | null;
  companySlug: string | null;
  actorId: string | null;
  actorUsername: string;
  actorRole: string;
  action: string;
  targetType: string;
  targetId: string;
  targetName: string | null;
  payloadJson: string | null;
  correlationId: string | null;
  createdAtUtc: string;
}
