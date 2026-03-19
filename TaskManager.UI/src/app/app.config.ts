import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
// Додали withInterceptors сюди:
import { provideHttpClient, withInterceptors } from '@angular/common/http';
// Підключаємо наш новий перехоплювач:
import { authInterceptor } from './services/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    // Вмикаємо перехоплювач для всіх HTTP-запитів:
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
