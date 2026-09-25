export function formatTurkishDate(value?: string | null): string {
  if (!value) return 'Tarih yok';

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return 'Tarih yok';
  }

  return new Intl.DateTimeFormat('tr-TR', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  }).format(date);
}
