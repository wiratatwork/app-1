import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="min-height:100vh;display:flex;align-items:center;justify-content:center;padding:16px;">
      <div class="card" style="max-width:420px;width:100%;">
        @if (checkingSession()) {
          <h2 style="margin-top:0;">App-1 Login</h2>
          <p>Checking session...</p>
        } @else {
          <h2 style="margin-top:0;">App-1 Login</h2>
          <p>Use Keycloak SSO from Factory Portal.</p>
          @if (errorMessage()) {
            <p style="color:#b91c1c;">{{ errorMessage() }}</p>
          }
          <button (click)="login()" [disabled]="loading()" style="padding:10px 14px;">{{ loading() ? 'Redirecting...' : 'Login with Keycloak' }}</button>
        }
      </div>
    </div>
  `,
})
export class LoginComponent implements OnInit {
  readonly loading = signal(false);
  readonly checkingSession = signal(false);
  readonly errorMessage = signal<string | null>(null);

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
  ) {}

  async ngOnInit(): Promise<void> {
    if (this.authService.isAuthenticated()) {
      await this.router.navigate(['/'], { replaceUrl: true });
      return;
    }

    if (this.route.snapshot.queryParamMap.get('sso') === '1') {
      this.authService.clearSilentSsoAttempted();
    }

    if (this.authService.hasAttemptedSilentSso()) {
      return;
    }

    this.checkingSession.set(true);
    try {
      const redirecting = await this.authService.trySilentSsoLogin();
      if (!redirecting) {
        this.checkingSession.set(false);
      }
    } catch (e) {
      const message = e instanceof Error ? e.message : 'unknown error';
      this.errorMessage.set(`Cannot check Keycloak session: ${message}`);
      this.checkingSession.set(false);
    }
  }

  async login(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set(null);
    try {
      await this.authService.redirectToLogin();
    } catch (e) {
      const message = e instanceof Error ? e.message : 'unknown error';
      this.errorMessage.set(`Cannot open Keycloak login: ${message}`);
      this.loading.set(false);
    }
  }
}
