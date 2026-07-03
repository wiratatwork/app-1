import { Routes } from '@angular/router';
import { HomeComponent } from './home.component';
import { LoginComponent } from './auth/login.component';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: '', canActivate: [authGuard], component: HomeComponent },
  { path: '**', redirectTo: '' },
];
