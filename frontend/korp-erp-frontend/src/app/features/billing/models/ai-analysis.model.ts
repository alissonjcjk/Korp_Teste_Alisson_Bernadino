export type AiRiskLevel = 'low' | 'medium' | 'high' | 'unavailable';

export interface InvoiceAiAnalysisResponse {
  isAvailable: boolean;
  hasAnomalies: boolean;
  riskLevel: AiRiskLevel;
  summary: string;
  risks: string[];
  suggestions: string[];
  provider: string;
  analyzedAt: string;
}
