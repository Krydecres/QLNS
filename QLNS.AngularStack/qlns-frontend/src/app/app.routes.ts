import { Routes } from '@angular/router';
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

export const routes: Routes = [
    {
        path: '',
        component: LayoutComponent,
        children: [
            { path: '', component: HomeComponent },
            { path: 'login', component: LoginComponent },
            { path: 'register', component: RegisterComponent },
            { path: 'admin-dashboard', component: AdminDashboardComponent },
            { path: 'employee-dashboard', component: EmployeeDashboardComponent },
            { path: 'timekeeping/list', component: TimekeepingListComponent },
            { path: 'timekeeping/my-attendance', component: AttendanceBoardComponent },
            { path: 'timekeeping/manual-entry', component: ManualEntryComponent },

            { path: 'departments', component: DepartmentListComponent },
            { path: 'departments/create', component: DepartmentFormComponent },
            { path: 'departments/edit/:id', component: DepartmentFormComponent },
            { path: 'departments/details/:id', component: DepartmentDetailsComponent },

            { path: 'positions', component: PositionListComponent },
            { path: 'positions/create', component: PositionFormComponent },
            { path: 'positions/edit/:id', component: PositionFormComponent },
            { path: 'positions/details/:id', component: PositionDetailsComponent },

            { path: 'employees', component: EmployeeListComponent },
            { path: 'employees/create', component: EmployeeFormComponent },
            { path: 'employees/edit/:id', component: EmployeeFormComponent },
            { path: 'employees/pending-updates', component: PendingUpdatesComponent },
            { path: 'employees/my-profile', component: MyProfileComponent },
            { path: 'employees/my-department', component: MyDepartmentComponent }
        ]
    }
];
