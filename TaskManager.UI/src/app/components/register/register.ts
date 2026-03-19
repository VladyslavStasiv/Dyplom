import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class RegisterComponent {
  userData = { username: '', email: '', password: '' };
  errorMessage = '';
  successMessage = '';

  constructor(private readonly authService: AuthService, private readonly router: Router) {}

  register() {
    if (!this.userData.username || !this.userData.email || !this.userData.password) {
      this.errorMessage = 'Будь ласка, заповніть всі поля!';
      return;
    }

    this.authService.register(this.userData).subscribe({
      next: (response: any) => {
        // Якщо все ок - показуємо зелене повідомлення і через 2 секунди кидаємо на логін
        this.successMessage = 'Реєстрація успішна! Переходимо до входу...';
        this.errorMessage = '';
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: (err: any) => {
        this.errorMessage = 'Помилка реєстрації. Можливо, такий email вже існує.';
        this.successMessage = '';
        console.error('Помилка:', err);
      }
    });
  }
}
