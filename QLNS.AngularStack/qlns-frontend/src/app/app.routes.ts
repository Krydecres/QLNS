import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

import { LayoutComponent } from './core/layout/layout.component';
import { HomeComponent } from './features/home/home.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';

import { AdminDashboardComponent } from './features/dashboard/admin-dashboard/admin-dashboard.component';
import { EmployeeDashboardComponent } from './features/dashboard/employee-dashboard/employee-dashboard.component';

import { AttendanceBoardComponent } from './features/timekeeping/attendance-board/attendance-board.component';
import { ManualEntryComponent } from './features/timekeeping/manual-entry/manual-entry.component';
import { TimekeepingListComponent } from './features/timekeeping/timekeeping-list/timekeeping-list.component';

import { DepartmentListComponent } from './features/department/department-list/department-list.component';
import { DepartmentFormComponent } from './features/department/department-form/department-form.component';
import { DepartmentDetailsComponent } from './features/department/department-details/department-details.component';

import { PositionListComponent } from './features/position/position-list/position-list.component';
import { PositionFormComponent } from './features/position/position-form/position-form.component';
import { PositionDetailsComponent } from './features/position/position-details/position-details.component';

import { EmployeeListComponent } from './features/employee/employee-list/employee-list.component';
import { EmployeeFormComponent } from './features/employee/employee-form/employee-form.component';
import { PendingUpdatesComponent } from './features/employee/pending-updates/pending-updates.component';
import { MyProfileComponent } from './features/employee/my-profile/my-profile.component';
import { MyDepartmentComponent } from './features/employee/my-department/my-department.component';

import { MyLeavesComponent } from './features/leave/my-leaves/my-leaves.component';
import { LeaveFormComponent } from './features/leave/leave-form/leave-form.component';
import { LeaveApprovalComponent } from './features/leave/leave-approval/leave-approval.component';

export const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: '', component: HomeComponent },
      { path: 'login', component: LoginComponent },
      { path: 'register', component: RegisterComponent },

      {
        path: 'admin-dashboard',
        component: AdminDashboardComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'employee-dashboard',
        component: EmployeeDashboardComponent,
        canActivate: [authGuard],
        data: { roles: ['Employee'] }
      },

      {
        path: 'timekeeping/list',
        component: TimekeepingListComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'timekeeping/manual-entry',
        component: ManualEntryComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'timekeeping/my-attendance',
        component: AttendanceBoardComponent,
        canActivate: [authGuard],
        data: { roles: ['Employee'] }
      },

      {
        path: 'departments',
        component: DepartmentListComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'departments/create',
        component: DepartmentFormComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'departments/edit/:id',
        component: DepartmentFormComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'departments/details/:id',
        component: DepartmentDetailsComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },

      {
        path: 'positions',
        component: PositionListComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'positions/create',
        component: PositionFormComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'positions/edit/:id',
        component: PositionFormComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'positions/details/:id',
        component: PositionDetailsComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },

      {
        path: 'employees',
        component: EmployeeListComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'employees/create',
        component: EmployeeFormComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'employees/edit/:id',
        component: EmployeeFormComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'employees/pending-updates',
        component: PendingUpdatesComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      },
      {
        path: 'employees/my-profile',
        component: MyProfileComponent,
        canActivate: [authGuard],
        data: { roles: ['Employee'] }
      },
      {
        path: 'employees/my-department',
        component: MyDepartmentComponent,
        canActivate: [authGuard],
        data: { roles: ['Employee'] }
      },
      {
        path: 'leave/my-leaves',
        component: MyLeavesComponent,
        canActivate: [authGuard],
        data: { roles: ['Employee'] }
      },
      {
        path: 'leave/create',
        component: LeaveFormComponent,
        canActivate: [authGuard],
        data: { roles: ['Employee'] }
      },
      {
        path: 'leave/approval',
        component: LeaveApprovalComponent,
        canActivate: [authGuard],
        data: { roles: ['Admin'] }
      }
    ]
  }
];