export type Role = 'Requester' | 'Agent' | 'Admin';
export type Status = 'Open' | 'InProgress' | 'Pending' | 'Resolved' | 'Closed' | 'Reopened';
export interface User { id: string; fullName: string; email: string; role: Role; isActive: boolean; }
export interface AuthResponse { accessToken: string; refreshToken: string; expiresAt: string; user: User; }
export interface Category { id: number; name: string; description: string; isActive: boolean; }
export interface Priority { id: number; name: string; level: number; responseTimeMinutes: number; resolutionTimeMinutes: number; }
export interface Agent { id: string; fullName: string; }
export interface Ticket { id: number; ticketNumber: string; title: string; description: string; categoryId: number; category: string; priorityId: number; priority: string; status: Status; requesterId: string; requesterName: string; assigneeId: string | null; assigneeName: string | null; createdAt: string; updatedAt: string; dueDate: string; }
export interface Page<T> { items: T[]; total: number; page: number; pageSize: number; }
export interface TicketDetail { ticket: Ticket; comments: { id: number; authorName: string; content: string; isInternal: boolean; createdAt: string }[]; activities: { id: number; actorName: string; activityType: string; oldValue: string | null; newValue: string | null; createdAt: string }[]; }
export interface Summary { total: number; active: number; resolved: number; overdue: number; unassigned: number; mine: number; byStatus: { status: Status; count: number }[]; byCategory: { name: string; count: number }[]; }
export const statusLabels: Record<Status, string> = { Open: 'Terbuka', InProgress: 'Diproses', Pending: 'Menunggu', Resolved: 'Selesai', Closed: 'Ditutup', Reopened: 'Dibuka kembali' };
