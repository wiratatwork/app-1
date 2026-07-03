import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  if (authService.isAuthenticated()) return true;
  if (await authService.refreshSession()) return true;

  if (route.queryParamMap.get('sso') === '1') {
    authService.clearSilentSsoAttempted();
  }

  if (!authService.hasAttemptedSilentSso()) {
    await authService.trySilentSsoLogin();
    return false;
  }

  return router.createUrlTree(['/login']);
};
