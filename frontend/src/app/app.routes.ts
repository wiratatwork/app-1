import { Routes } from '@angular/router';
import { HomeComponent } from './home.component';
import { LoginComponent } from './auth/login.component';
import { CallbackComponent } from './auth/callback.component';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'auth/callback', component: CallbackComponent },
  { path: '', canActivate: [authGuard], component: HomeComponent },
  { path: '**', redirectTo: '' },
];
