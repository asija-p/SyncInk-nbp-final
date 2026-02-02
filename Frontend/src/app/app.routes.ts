import { Routes } from '@angular/router';
import { Login } from './auth/login/login';
import { Register } from './auth/register/register';
import { Home } from './home/home';
import { Room } from './room/room';
import { NotFound } from './shared/components/not-found/not-found';
import { AuthGuard } from './auth/auth-guard';
import { ProfilePage } from './profile-page/profile-page';

export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },

  {
    path: 'auth',
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'login', component: Login },
      { path: 'register', component: Register },
    ],
  },

  { path: 'room/:roomId', component: Room, canActivate: [AuthGuard] },
  { path: 'profile', component: ProfilePage, canActivate: [AuthGuard] },
  { path: 'home', component: Home },
  { path: '404', component: NotFound },
  { path: '**', redirectTo: '/home' },
];
