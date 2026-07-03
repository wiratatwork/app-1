import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.component.html',
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
    if (await this.authService.ensureSession()) {
      await this.router.navigate(['/'], { replaceUrl: true });
      return;
    }

    if (this.route.snapshot.queryParamMap.get('sso') === '1') {
      this.authService.clearSilentSsoAttempted();
    }

    if (this.route.snapshot.queryParamMap.get('sso') === 'failed') {
      return;
    }

    if (this.authService.hasAttemptedSilentSso()) {
      return;
    }

    this.checkingSession.set(true);
    this.authService.trySilentSsoLogin();
  }

  login(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.authService.redirectToLogin();
  }
}
