import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from './auth/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="padding:24px;max-width:760px;margin:0 auto;">
      <div class="card">
        <h1 style="margin-top:0;">Web App (app-1)</h1>
        <p style="margin:0 0 12px;">You are already logged in via Keycloak SSO.</p>
        <p style="margin:0;"><strong>Current user:</strong> {{ userDisplayName() }}</p>
      </div>
      <div style="margin-top:16px;">
        <button (click)="logout()" style="padding:10px 14px;">Sign out</button>
      </div>
    </div>
  `,
})
export class HomeComponent implements OnInit {
  readonly userDisplayName = signal('Unknown');

  constructor(private readonly authService: AuthService) {}

  async ngOnInit(): Promise<void> {
    await this.authService.ensureSession();
    this.userDisplayName.set(this.authService.getUserDisplayName());
  }

  logout(): void {
    this.authService.logout();
  }
}
