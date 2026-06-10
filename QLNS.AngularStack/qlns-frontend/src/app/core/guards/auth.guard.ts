import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const currentRole = auth.role();
  const allowedRoles = route.data?.['roles'] as string[] | undefined;

  if (!currentRole) {
    router.navigate(['/login'], {
      queryParams: { returnUrl: state.url }
    });

    return false;
  }

  if (allowedRoles && !allowedRoles.includes(currentRole)) {
    if (currentRole === 'Admin') {
      router.navigate(['/admin-dashboard']);
    } else if (currentRole === 'Employee') {
      router.navigate(['/employee-dashboard']);
    } else {
      router.navigate(['/login']);
    }

    return false;
  }

  return true;
};