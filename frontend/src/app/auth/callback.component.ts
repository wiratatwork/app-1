import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  standalone: true,
  template: `<div style="padding:24px;">Completing login...</div>`,
})
export class CallbackComponent implements OnInit {
  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  async ngOnInit(): Promise<void> {
    const result = await this.authService.completeLoginFromCallback(window.location.search);
    if (result === 'success') {
      await this.router.navigate(['/'], { replaceUrl: true });
      return;
    }

    await this.router.navigate(['/login'], { replaceUrl: true });
  }
}
