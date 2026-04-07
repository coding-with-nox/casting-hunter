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
  scrapedAt: string;
}

export interface SourceStatus {
  name: string;
  region: SourceRegion;
  isEnabled: boolean;
  lastScrapedAt: string | null;
  errorCount: number;
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

export interface FilterState {
  keywords?: string;
  types?: CastingType[];
  regions?: SourceRegion[];
  onlyPaid?: boolean;
  gender?: string;
}
