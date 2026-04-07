import type { SourceRegion } from '../api/types';

const regionColors: Record<SourceRegion, string> = {
  Italy: 'bg-green-900 text-green-300',
  Europe: 'bg-blue-900 text-blue-300',
  International: 'bg-purple-900 text-purple-300',
};

interface Props {
  name: string;
  region: SourceRegion;
}

export function SourceBadge({ name, region }: Props) {
  return (
    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${regionColors[region]}`}>
      {name}
    </span>
  );
}
