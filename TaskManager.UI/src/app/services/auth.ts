import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = 'http://localhost:5133/api/Users';

  constructor(private readonly http: HttpClient) { }

  // Реєстрація
  register(userData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, userData);
  }

  // Вхід в систему
  login(credentials: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, credentials);
  }

  // Збереження токена в localStorage
  saveToken(token: string): void {
    localStorage.setItem('jwt_token', token);
  }

  // Отримання токена
  getToken(): string | null {
    return localStorage.getItem('jwt_token');
  }

  // Перевірка активної сесії
  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  // Вихід (очищення сесії)
  logout(): void {
    localStorage.removeItem('jwt_token');
  }

  // Декодування імені з JWT токена (з підтримкою кирилиці)
  getUsername(): string {
    const token = this.getToken();
    if (!token) return 'Гість';

    try {
      // Дістаємо payload (другу частину токена)
      const payload = token.split('.')[1];

      // Виправляємо специфічні символи Base64Url для коректного парсингу (використовуємо сучасний replaceAll)
      const base64 = payload.replaceAll('-', '+').replaceAll('_', '/');
      const binaryString = atob(base64);

      // Декодуємо байти в UTF-8 для правильного відображення української мови
      const bytes = new Uint8Array(binaryString.length);
      for (let i = 0; i < binaryString.length; i++) {
        // Використовуємо сучасний codePointAt замість charCodeAt (додаємо ?? 0 для безпеки типів)
        bytes[i] = binaryString.codePointAt(i) ?? 0;
      }
      const decodedJson = new TextDecoder('utf-8').decode(bytes);
      const decodedData = JSON.parse(decodedJson);

      // Витягуємо ім'я зі стандартних полів .NET або загальних полів JWT
      return decodedData['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
          || decodedData.unique_name
          || decodedData.name
          || 'Користувач';

    } catch (error) {
      console.error('Помилка розшифровки токена:', error);
      return 'Користувач';
    }
  }
}
