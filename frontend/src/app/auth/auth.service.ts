import { Injectable, signal } from '@angular/core';

export interface BffUserInfo {
  subject: string;
  username?: string;
  email?: string;
  name?: string;
}

interface SessionStatusResponse {
  authenticated: boolean;
  user?: BffUserInfo;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authenticated = signal(false);
  private readonly currentUser = signal<BffUserInfo | null>(null);
  private silentSsoAttempted = false;

  readonly isAuthenticatedSignal = this.authenticated.asReadonly();
  readonly currentUserSignal = this.currentUser.asReadonly();

  isAuthenticated(): boolean {
    return this.authenticated();
  }

  getUserDisplayName(): string {
    const user = this.currentUser();
    return user?.name || user?.username || user?.email || user?.subject || 'Unknown';
  }

  hasAttemptedSilentSso(): boolean {
    return this.silentSsoAttempted;
  }

  clearSilentSsoAttempted(): void {
    this.silentSsoAttempted = false;
  }

  async ensureSession(): Promise<boolean> {
    try {
      const response = await fetch('/api/bff/auth/session', {
        credentials: 'include',
        headers: { Accept: 'application/json' },
      });
      if (!response.ok) {
        this.clearLocalState();
        return false;
      }

      const data = (await response.json()) as SessionStatusResponse;
      if (!data.authenticated || !data.user) {
        this.clearLocalState();
        return false;
      }

      this.authenticated.set(true);
      this.currentUser.set(data.user);
      return true;
    } catch {
      this.clearLocalState();
      return false;
    }
  }

  trySilentSsoLogin(): void {
    if (this.isAuthenticated() || this.silentSsoAttempted) {
      return;
    }

    this.silentSsoAttempted = true;
    window.location.assign('/api/bff/auth/silent');
  }

  redirectToLogin(): void {
    this.clearSilentSsoAttempted();
    window.location.assign('/api/bff/auth/login');
  }

  logout(): void {
    this.clearLocalState();
    this.clearSilentSsoAttempted();
    window.location.assign('/api/bff/auth/logout');
  }

  private clearLocalState(): void {
    this.authenticated.set(false);
    this.currentUser.set(null);
  }
}
