import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
// НОВЕ: Підключаємо RouterOutlet та наш сервіс авторизації
import { RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth';

@Component({
  selector: 'app-root',
  standalone: true,
  // НОВЕ: Додали RouterOutlet у список imports
  imports: [CommonModule, FormsModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit {
  tasks: any[] = [];

  // Змінна, яка керує тим, чи показувати форму
  showForm = false;

  editingTaskId: number | null = null;
  username: string = '';

  // Тут ми будемо тимчасово зберігати дані з полів вводу
  newTask = {
    title: '',
    description: '',
    deadline: '',
    complexity: 5, // За замовчуванням середня складність
    columnId: 1 // Поки всі нові задачі кидаємо в першу колонку
  };

  constructor(
    private readonly http: HttpClient,
    private readonly cdr: ChangeDetectorRef,
    // НОВЕ: Додали public authService у конструктор
    public authService: AuthService
  ) {}

  ngOnInit() {
    this.username = this.authService.getUsername();
    this.fetchTasks();
  }

  fetchTasks() {
    this.http.get<any[]>('http://localhost:5133/api/tasks').subscribe({
      next: (data) => {
        this.tasks = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Помилка підключення:', err);
      }
    });
  }

  // Відкриває форму і заповнює її даними існуючої задачі
  openEditForm(task: any) {
    this.editingTaskId = task.id;
    this.newTask = { ...task }; // Копіюємо дані задачі

    // Форматуємо дату для HTML-поля (відрізаємо секунди)
    if (this.newTask.deadline) {
      this.newTask.deadline = new Date(this.newTask.deadline).toISOString().slice(0, 16);
    }
    this.showForm = true;
  }

  // Очищає і ховає форму
  resetForm() {
    this.showForm = false;
    this.editingTaskId = null;
    this.newTask = { title: '', description: '', deadline: '', complexity: 5, columnId: 1 };
  }

  saveTask() {
    if (!this.newTask.title) {
      alert('Будь ласка, введіть назву задачі!');
      return;
    }

    if (this.editingTaskId) {
      // РЕДАГУВАННЯ (PUT запит)
      const updatedTask = { ...this.newTask, id: this.editingTaskId };
      this.http.put(`http://localhost:5133/api/tasks/${this.editingTaskId}`, updatedTask).subscribe({
        next: () => {
          this.fetchTasks();
          this.resetForm();
        },
        error: (err) => console.error('Помилка оновлення:', err)
      });
    } else {
      // СТВОРЕННЯ (POST запит)
      this.http.post<any>('http://localhost:5133/api/tasks', this.newTask).subscribe({
        next: () => {
          this.fetchTasks();
          this.resetForm();
        },
        error: (err) => console.error('Помилка створення:', err)
      });
    }
  }

  // Метод для видалення задачі
  deleteTask(id: number) {
    // Просимо підтвердження, щоб не видалити випадково
    if (confirm('Ви впевнені, що хочете видалити цю задачу?')) {
      this.http.delete(`http://localhost:5133/api/tasks/${id}`).subscribe({
        next: () => {
          this.fetchTasks(); // Оновлюємо список після видалення
        },
        error: (err) => {
          console.error('Помилка видалення:', err);
        }
      });
    }
  }

  // Метод для оновлення статусу (переміщення в іншу колонку)
  moveTask(task: any, newColumnId: number) {
    // Створюємо копію задачі з новим ColumnId
    const updatedTask = { ...task, columnId: newColumnId };

    // Відправляємо PUT-запит на наш бекенд
    this.http.put(`http://localhost:5133/api/tasks/${task.id}`, updatedTask).subscribe({
      next: () => {
        this.fetchTasks(); // Оновлюємо дошку, щоб побачити зміни
      },
      error: (err) => {
        console.error('Помилка оновлення задачі:', err);
      }
    });
  }

  // ЗМІННА: Зберігає ID задачі, яку ми зараз тримаємо мишкою
  draggedTaskId: number | null = null;

  // МЕТОД 1: Спрацьовує в момент, коли ми "схопили" картку
  onDragStart(task: any) {
    this.draggedTaskId = task.id;
  }

  // МЕТОД 2: Потрібен, щоб браузер дозволив "кинути" елемент у колонку
  onDragOver(event: DragEvent) {
    event.preventDefault(); // Вимикаємо стандартну поведінку браузера (яка блокує drop)
  }

  // МЕТОД 3: Спрацьовує, коли ми відпускаємо кнопку миші над новою колонкою
  onDrop(event: DragEvent, newColumnId: number) {
    event.preventDefault();

    if (this.draggedTaskId) {
      // Знаходимо в нашому масиві задачу, яку тягнули
      const task = this.tasks.find(t => t.id === this.draggedTaskId);

      // Якщо ми дійсно перетягнули її в ІНШУ колонку, викликаємо наш старий добрий moveTask
      if (task && task.columnId !== newColumnId) {
        this.moveTask(task, newColumnId);
      }

      // Після успішного кидка очищаємо "пам'ять"
      this.draggedTaskId = null;
    }
  }

  // НОВЕ: Метод для виходу з акаунта
  logout() {
    this.authService.logout();
  }
}
