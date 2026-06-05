import { Routes } from '@angular/router';
import { LayoutComponent } from './core/layout/layout.component';
import { HomeComponent } from './features/home/home.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { AdminDashboardComponent } from './features/dashboard/admin-dashboard/admin-dashboard.component';
import { EmployeeDashboardComponent } from './features/dashboard/employee-dashboard/employee-dashboard.component';

export const routes: Routes = [
    {
        path: '',
        component: LayoutComponent,
        children: [
            { path: '', component: HomeComponent },
            { path: 'login', component: LoginComponent },
            { path: 'register', component: RegisterComponent },
            { path: 'admin-dashboard', component: AdminDashboardComponent },
            { path: 'employee-dashboard', component: EmployeeDashboardComponent }
        ]
    }
];
