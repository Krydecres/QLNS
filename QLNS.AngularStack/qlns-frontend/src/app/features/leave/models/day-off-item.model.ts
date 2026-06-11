export interface DayOffItem {
  type: string;
  date: string;
  endDate?: string;
  title: string;
  description?: string;
  badgeClass: string;
  icon: string;
}

export interface MyDaysOffResponse {
  upcomingDays: DayOffItem[];
  pastDays: DayOffItem[];
  currentYear: number;
  totalHolidays: number;
  totalLeaves: number;
}
