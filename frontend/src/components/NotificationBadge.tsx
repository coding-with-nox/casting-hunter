interface Props {
  count: number;
}

export function NotificationBadge({ count }: Props) {
  if (count <= 0) return null;
  return (
    <span className="inline-flex items-center justify-center bg-[#d4af37] text-black text-xs font-bold rounded-full w-5 h-5 ml-1">
      {count > 99 ? '99+' : count}
    </span>
  );
}
