export type CastingType = 'Film' | 'TV' | 'Teatro' | 'Pubblicità' | 'Cortometraggio' | 'Internazionale' | 'Altro';
export type SourceRegion = 'Italy' | 'Europe' | 'International';

export interface CastingCall {
  id: string;
  title: string;
  description: string;
  sourceUrl: string;
  sourceName: string;
  type: CastingType;
  region: SourceRegion;
  location: string | null;
  deadline: string | null;
  isPaid: boolean;
  requiredGender: string | null;
  ageRange: string | null;
  requirements: string | null;
  isFavorite: boolean;
  isApplied: boolean;
  isHidden: boolean;
  scrapedAt: string;
}

export interface SourceStatus {
  name: string;
  region: SourceRegion;
  url: string | null;
  isEnabled: boolean;
  lastScrapedAt: string | null;
  errorCount: number;
  hasCustomScraper: boolean;
}

export interface UserProfile {
  preferredTypes: CastingType[];
  preferredRegions: SourceRegion[];
  scenicAge: number | null;
  gender: string;
  telegramChatId: string | null;
}

export interface Stats {
  total: number;
  newToday: number;
  bySource: Record<string, number>;
}

export interface BandiPriorityStep {
  title: string;
  detail: string;
}

export interface BandiSourceGroup {
  name: string;
  items: string[];
}

export interface Bando {
  id: string;
  title: string;
  issuerName: string;
  issuerType: string;
  sourceName: string;
  sourceUrl: string;
  applicationUrl: string | null;
  publishedAt: string | null;
  deadline: string | null;
  location: string | null;
  discipline: string | null;
  role: string | null;
  bodyText: string;
  isPublic: boolean;
  confidenceScore: number;
  status: string;
  createdAt: string;
  reviewSignals: string[];
}

export interface BandoSource {
  name: string;
  category: string;
  baseUrl: string;
  priority: number;
  isOfficial: boolean;
  isEnabled: boolean;
  lastRunAt: string | null;
  lastRunFound: number;
  lastRunEligible: number;
  lastRunNew: number;
  lastRunError: string | null;
}

export interface BandoScrapeResult {
  totalFound: number;
  totalEligible: number;
  totalNew: number;
  sources: string[];
}

export interface BandiPlan {
  status: string;
  summary: string;
  priorityPlan: BandiPriorityStep[];
  extractionFlow: string[];
  issuerTypes: string[];
  sourceGroups: BandiSourceGroup[];
}

export interface FilterState {
  keywords?: string;
  types?: CastingType[];
  regions?: SourceRegion[];
  onlyPaid?: boolean;
  gender?: string;
  showHidden?: boolean;
}
