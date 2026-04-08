import type { CastingCall, FilterState, SourceStatus, Stats, UserProfile } from './types';

const BASE = '/api';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const castingApi = {
  getCastings(filter?: FilterState): Promise<CastingCall[]> {
    const params = new URLSearchParams();
    if (filter?.keywords) params.set('keywords', filter.keywords);
    if (filter?.types?.length) params.set('types', filter.types.join(','));
    if (filter?.regions?.length) params.set('regions', filter.regions.join(','));
    if (filter?.onlyPaid) params.set('onlyPaid', 'true');
    if (filter?.gender) params.set('gender', filter.gender);
    if (filter?.showHidden) params.set('showHidden', 'true');
    const qs = params.toString();
    return request<CastingCall[]>(`/castings${qs ? `?${qs}` : ''}`);
  },

  getCasting(id: string): Promise<CastingCall> {
    return request<CastingCall>(`/castings/${id}`);
  },

  toggleFavorite(id: string): Promise<void> {
    return request<void>(`/castings/${id}/favorite`, { method: 'POST' });
  },

  markApplied(id: string): Promise<void> {
    return request<void>(`/castings/${id}/applied`, { method: 'POST' });
  },

  unmarkApplied(id: string): Promise<void> {
    return request<void>(`/castings/${id}/applied`, { method: 'DELETE' });
  },

  toggleHidden(id: string): Promise<void> {
    return request<void>(`/castings/${id}/hidden`, { method: 'POST' });
  },

  getSources(): Promise<SourceStatus[]> {
    return request<SourceStatus[]>('/sources');
  },

  toggleSourceEnabled(name: string, enabled: boolean): Promise<void> {
    return request<void>(`/sources/${encodeURIComponent(name)}/enabled?enabled=${enabled}`, { method: 'PATCH' });
  },

  deleteSource(name: string): Promise<void> {
    return request<void>(`/sources/${encodeURIComponent(name)}`, { method: 'DELETE' });
  },

  updateSource(name: string, data: { url?: string; region?: string }): Promise<SourceStatus> {
    return request<SourceStatus>(`/sources/${encodeURIComponent(name)}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  },

  createSource(name: string, url: string, region: string): Promise<SourceStatus> {
    return request<SourceStatus>('/sources', {
      method: 'POST',
      body: JSON.stringify({ name, url, region }),
    });
  },

  scrapeAll(): Promise<{ totalFound: number; totalNew: number }> {
    return request('/sources/scrape-all', { method: 'POST' });
  },

  scrapeSource(name: string): Promise<{ totalFound: number; totalNew: number }> {
    return request(`/sources/${name}/scrape`, { method: 'POST' });
  },

  getProfile(): Promise<UserProfile> {
    return request<UserProfile>('/profile');
  },

  updateProfile(profile: Partial<UserProfile>): Promise<UserProfile> {
    return request<UserProfile>('/profile', {
      method: 'PUT',
      body: JSON.stringify(profile),
    });
  },

  getStats(): Promise<Stats> {
    return request<Stats>('/stats');
  },
};
