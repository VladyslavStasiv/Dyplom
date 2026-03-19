import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // Адреса нашого бекенду для користувачів
  private readonly apiUrl = 'http://localhost:5133/api/Users';

  constructor(private readonly http: HttpClient) { }

  // 1. Реєстрація нового користувача
  register(userData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, userData);
  }

  // 2. Вхід (Логін)
  login(credentials: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, credentials);
  }

  // 3. Збереження токена в "сейф" браузера
  saveToken(token: string): void {
    localStorage.setItem('jwt_token', token);
  }

  // 4. Отримання токена
  getToken(): string | null {
    return localStorage.getItem('jwt_token');
  }

  // 5. Перевірка, чи користувач зараз залогінений
  isLoggedIn(): boolean {
    return !!this.getToken(); // Повертає true, якщо токен існує
  }

  // 6. Вихід з системи (просто видаляємо токен)
  logout(): void {
    localStorage.removeItem('jwt_token');
  }

  // Метод для витягування імені користувача з JWT токена
  getUsername(): string {
    const token = this.getToken(); // Беремо токен з пам'яті браузера

    // Якщо токена немає (користувач не залогінений), повертаємо стандартне слово
    if (!token) return 'Гість';

    try {
      // JWT токен складається з 3 частин, розділених крапкою (.).
      // Нам потрібна друга частина (індекс 1), де лежить так званий "payload" (корисне навантаження)
      const payload = token.split('.')[1];

      // Розшифровуємо рядок з формату Base64
      const decodedJson = atob(payload);

      // Перетворюємо розшифрований текст у повноцінний JavaScript-об'єкт
      const decodedData = JSON.parse(decodedJson);

      // C# зазвичай зберігає ім'я (Username) у спеціальному полі з довгою системною назвою.
      // Шукаємо його, або використовуємо короткі варіанти, якщо бекенд налаштовано інакше.
      // Якщо взагалі нічого не знайдено, виводимо 'Користувач'.
      const username = decodedData['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
                    || decodedData.unique_name
                    || decodedData.name
                    || 'Користувач';

      return username;
    } catch (error) {
      // Якщо токен пошкоджений або сталася помилка розшифровки, не ламаємо сайт, а просто повертаємо дефолтне ім'я
      console.error('Помилка розшифровки токена:', error);
      return 'Користувач';
    }
  }
}
