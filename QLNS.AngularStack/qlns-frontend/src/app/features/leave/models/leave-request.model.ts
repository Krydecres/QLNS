export enum LeaveRequestStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2
}

export interface LeaveRequest {
  id: number;
  employeeId?: number;
  employeeName?: string;
  startDate: string;
  endDate: string;
  reason: string;
  status: LeaveRequestStatus;
  createdAt: string;
  approvedById?: number;
  approvedByName?: string;
  approvalNote?: string;
}

export interface LeaveRequestCreateDto {
  startDate: string;
  endDate: string;
  reason: string;
}

export interface ProcessRequestDto {
  status: LeaveRequestStatus;
  note?: string;
  adminUsername: string;
}
