export interface Department {
    id: number;
    name: string;
}

export interface Position {
    id: number;
    name: string;
}

export interface Employee {
    id: number;
    fullName: string;
    email: string;
    phoneNumber?: string;
    dateOfBirth?: string;
    departmentId?: number;
    positionId?: number;
    department?: Department;
    position?: Position;
}

export interface ProfileUpdateRequest {
    id: number;
    employeeId: number;
    newPhoneNumber?: string;
    newDateOfBirth?: string;
    status: string;
    createdAt: string;
    employee?: Employee;
}
