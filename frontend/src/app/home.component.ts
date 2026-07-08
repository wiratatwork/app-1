import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AuthService } from './auth/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit {
  private readonly authService = inject(AuthService);

  readonly mobileSidebarOpen = signal(false);
  readonly user = this.authService.currentUserSignal;
  readonly userDisplayName = computed(() => this.authService.getUserDisplayName());
  readonly userEmail = computed(() => this.user()?.email ?? 'No email available');
  readonly userInitials = computed(() => {
    const displayName = this.userDisplayName().trim();
    if (!displayName) {
      return 'U';
    }

    const parts = displayName
      .split(' ')
      .map((part) => part.trim())
      .filter(Boolean);
    if (parts.length === 0) {
      return displayName.slice(0, 2).toUpperCase();
    }

    if (parts.length === 1) {
      return parts[0].slice(0, 2).toUpperCase();
    }

    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  });
  readonly navigationItems = [
    { label: 'Overview', icon: 'OV', active: true },
    { label: 'Users', icon: 'US', active: false },
    { label: 'Reports', icon: 'RP', active: false },
    { label: 'Settings', icon: 'ST', active: false },
  ];
  readonly dashboardStats = [
    { label: 'Machines Online', value: '126', trend: '+4 from last shift' },
    { label: 'Active Anomalies', value: '7', trend: '2 critical / 5 warning' },
    { label: 'Downtime (Today)', value: '42m', trend: '-18% vs yesterday' },
    { label: 'Prediction Risk', value: 'Medium', trend: 'Bearing Line-2 in 6h' },
  ];
  readonly anomalyMachines = [
    {
      machine: 'Press-07',
      zone: 'Zone A - Stamping',
      issue: 'Vibration spike over threshold',
      status: 'critical',
      detectedAt: '14:08',
    },
    {
      machine: 'Conveyor-12',
      zone: 'Zone B - Transfer',
      issue: 'Motor temperature trending high',
      status: 'warning',
      detectedAt: '13:54',
    },
    {
      machine: 'CNC-04',
      zone: 'Zone C - Machining',
      issue: 'Tool wear anomaly detected',
      status: 'warning',
      detectedAt: '13:41',
    },
    {
      machine: 'Boiler-02',
      zone: 'Utility Plant',
      issue: 'Pressure fluctuation unstable',
      status: 'critical',
      detectedAt: '13:33',
    },
  ];
  readonly mapPoints = [
    { label: 'Press-07', top: '28%', left: '32%', status: 'critical' },
    { label: 'Conveyor-12', top: '56%', left: '44%', status: 'warning' },
    { label: 'CNC-04', top: '38%', left: '64%', status: 'warning' },
    { label: 'Boiler-02', top: '72%', left: '18%', status: 'critical' },
    { label: 'Packing-03', top: '67%', left: '76%', status: 'normal' },
  ];

  async ngOnInit(): Promise<void> {
    await this.authService.ensureSession();
  }

  toggleMobileSidebar(): void {
    this.mobileSidebarOpen.update((open) => !open);
  }

  closeMobileSidebar(): void {
    this.mobileSidebarOpen.set(false);
  }

  logout(): void {
    this.authService.logout();
  }
}
