import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent {
  credentials = { email: '', password: '' };
  errorMessage = '';

  constructor(private readonly authService: AuthService, private readonly router: Router) {}

  login() {
    if (!this.credentials.email || !this.credentials.password) {
      this.errorMessage = 'Будь ласка, заповніть всі поля!';
      return;
    }

    this.authService.login(this.credentials).subscribe({
      next: (response: any) => {
        this.authService.saveToken(response.token);
        // Замість м'якого переходу просто перезавантажуємо сторінку на корінь
        globalThis.location.href = '/';
      },
      error: (err: any) => {
        this.errorMessage = 'Неправильний email або пароль!';
        console.error('Помилка входу:', err);
      }
    });
  }
}
