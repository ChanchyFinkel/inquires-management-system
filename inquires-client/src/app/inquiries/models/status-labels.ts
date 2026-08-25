// The server's StatusName/PriorityName values are stable English keys, not display text —
// the database and the API contract don't localize. This is the client's one place that maps
// each key to Hebrew for rendering; StatusId/PriorityId keep flowing through filtering and
// sorting untouched.
export const STATUS_LABELS_HE: Record<string, string> = {
  New: 'חדש',
  InProgress: 'בטיפול',
  Waiting: 'ממתין',
  Completed: 'הושלם',
};

export const PRIORITY_LABELS_HE: Record<string, string> = {
  Low: 'נמוכה',
  Medium: 'בינונית',
  High: 'גבוהה',
};

export function statusLabel(statusName: string): string {
  return STATUS_LABELS_HE[statusName] ?? statusName;
}

export function priorityLabel(priorityName: string): string {
  return PRIORITY_LABELS_HE[priorityName] ?? priorityName;
}

// 'InProgress' -> 'in-progress' — the shared key both the badge and the summary cards style by.
export function toKebabKey(name: string): string {
  return name.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();
}
