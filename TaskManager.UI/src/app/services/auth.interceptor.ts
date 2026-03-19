import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth'; // Твій сервіс авторизації

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  // Якщо токен є, клонуємо запит і додаємо заголовок Authorization
  if (token) {
    const clonedRequest = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(clonedRequest); // Відправляємо запит з токеном
  }

  // Якщо токена немає (наприклад, при логіні), відправляємо як є
  return next(req);
};
