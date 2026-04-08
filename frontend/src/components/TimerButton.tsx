interface Props {
  secondsLeft: number;
  totalSeconds: number;
  refreshing: boolean;
  onClick: () => void;
}

export function TimerButton({ secondsLeft, totalSeconds, refreshing, onClick }: Props) {
  const pct = ((totalSeconds - secondsLeft) / totalSeconds) * 100;
  const size = 32;
  const r = 13;
  const circ = 2 * Math.PI * r;
  const dash = circ * (1 - pct / 100);

  return (
    <button
      onClick={onClick}
      title={refreshing ? 'Aggiornamento in corso...' : `Prossimo aggiornamento tra ${secondsLeft}s — clicca per aggiornare ora`}
      className="relative flex items-center justify-center hover:opacity-80 transition-opacity"
      style={{ width: size, height: size }}
    >
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} className="-rotate-90">
        {/* Track */}
        <circle
          cx={size / 2} cy={size / 2} r={r}
          fill="none"
          stroke="#252525"
          strokeWidth="2"
        />
        {/* Progress */}
        <circle
          cx={size / 2} cy={size / 2} r={r}
          fill="none"
          stroke={refreshing ? '#888' : '#d4af37'}
          strokeWidth="2"
          strokeDasharray={`${circ}`}
          strokeDashoffset={`${dash}`}
          strokeLinecap="round"
          style={{ transition: 'stroke-dashoffset 0.9s linear' }}
        />
      </svg>
      {/* Numero al centro */}
      <span
        className="absolute text-[9px] font-bold leading-none"
        style={{ color: refreshing ? '#666' : '#d4af37' }}
      >
        {refreshing ? '⟳' : secondsLeft > 99 ? '99+' : secondsLeft}
      </span>
    </button>
  );
}
