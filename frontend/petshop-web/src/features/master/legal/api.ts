import { masterFetch } from "../auth/masterFetch";

export type LegalDocumentType = "terms" | "privacy";

export type LegalDocumentDto = {
  id: string;
  documentType: LegalDocumentType;
  version: string;
  content: string;
  isActive: boolean;
  publishedAtUtc: string;
  createdAtUtc: string;
};

export function fetchActiveLegalDocument(type: LegalDocumentType) {
  return masterFetch<LegalDocumentDto | null>(`/master/legal/${type}`);
}

export function fetchLegalDocumentHistory(type: LegalDocumentType) {
  return masterFetch<LegalDocumentDto[]>(`/master/legal/${type}/history`);
}

export function publishLegalDocument(type: LegalDocumentType, version: string, content: string) {
  return masterFetch<LegalDocumentDto>(`/master/legal/${type}`, {
    method: "POST",
    body: JSON.stringify({ version, content }),
  });
}
