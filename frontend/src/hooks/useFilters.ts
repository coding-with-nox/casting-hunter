import { useState } from 'react';
import type { CastingType, FilterState, SourceRegion } from '../api/types';

export function useFilters(initial?: FilterState) {
  const [filter, setFilter] = useState<FilterState>(initial ?? {});

  const setKeywords = (keywords: string) => setFilter(f => ({ ...f, keywords: keywords || undefined }));
  const toggleType = (type: CastingType) => setFilter(f => {
    const types = f.types ?? [];
    return { ...f, types: types.includes(type) ? types.filter(t => t !== type) : [...types, type] };
  });
  const toggleRegion = (region: SourceRegion) => setFilter(f => {
    const regions = f.regions ?? [];
    return { ...f, regions: regions.includes(region) ? regions.filter(r => r !== region) : [...regions, region] };
  });
  const setOnlyPaid = (onlyPaid: boolean) => setFilter(f => ({ ...f, onlyPaid }));
  const reset = () => setFilter({});

  return { filter, setKeywords, toggleType, toggleRegion, setOnlyPaid, reset };
}
