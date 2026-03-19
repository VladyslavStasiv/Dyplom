import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { RegisterComponent } from './components/register/register';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  // Якщо адреса порожня (просто localhost:4200), кидаємо на логін
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  // Якщо ввели якусь нісенітницю в URL - теж на логін
  { path: '**', redirectTo: 'login' }
];
