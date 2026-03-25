import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit {
  tasks: any[] = [];
  columns: any[] = [];
  boards: any[] = [];

  sharedUsers: any[] = [];
  showAccessModal = false;

  showShareModal = false;
  shareEmail = '';
  shareRole = 'Editor';

  searchQuery: string = '';

  // 💡 НОВЕ: Змінні для спливаючого опису (Tooltip)
  hoverTimeout: any;
  hoveredTaskId: number | null = null;

  get filteredTasks() {
    if (!this.searchQuery || this.searchQuery.trim() === '') {
      return this.tasks;
    }

    const lowerQuery = this.searchQuery.toLowerCase();

    return this.tasks.filter(t => {
      if (!t) return false;
      const safeTitle = (t.title || '').toLowerCase();
      const safeDesc = (t.description || '').toLowerCase();
      return safeTitle.includes(lowerQuery) || safeDesc.includes(lowerQuery);
    });
  }

  get todoCount() {
    return this.columns.length > 0 ? this.tasks.filter(t => t.columnId === this.columns[0].id).length : 0;
  }

  get inProgressCount() {
    return this.columns.length > 1 ? this.tasks.filter(t => t.columnId === this.columns[1].id).length : 0;
  }

  get doneCount() {
    return this.columns.length > 2 ? this.tasks.filter(t => t.columnId === this.columns[2].id).length : 0;
  }

  get totalTasksCount() {
    return this.todoCount + this.inProgressCount + this.doneCount;
  }

  get progressPercentage() {
    if (this.totalTasksCount === 0) return 0;
    return Math.round((this.doneCount / this.totalTasksCount) * 100);
  }

  get isCurrentBoardOwner(): boolean {
    if (!this.currentBoardId) return false;
    const board = this.boards.find(b => b.id == this.currentBoardId);
    return board?.owner?.username === this.username;
  }

  get currentRole(): string {
    if (this.isCurrentBoardOwner) return 'Owner';

    const board = this.boards.find(b => b.id == this.currentBoardId);
    if (board?.sharedWithUsers) {
      const myShare = board.sharedWithUsers.find((ub: any) => ub.user?.username === this.username);
      if (myShare) return myShare.accessLevel;
    }
    return 'Viewer';
  }

  get canEdit(): boolean {
    return this.currentRole === 'Owner' || this.currentRole === 'Editor';
  }

  currentBoardId: number | null = null;
  showForm = false;
  editingTaskId: number | null = null;
  username: string = '';

  newTask = {
    title: '',
    description: '',
    deadline: '',
    complexity: 5,
    columnId: 0
  };

  constructor(
    private readonly http: HttpClient,
    private readonly cdr: ChangeDetectorRef,
    public authService: AuthService
  ) {}

  ngOnInit() {
    this.username = this.authService.getUsername();
    this.fetchBoard();
    this.fetchTasks();
  }

  fetchBoard() {
    this.http.get<any[]>('http://localhost:5133/api/Boards').subscribe({
      next: (boards) => {
        if (boards && boards.length > 0) {
          this.boards = boards;
          this.selectBoard(boards[0].id);
        }
      },
      error: (err) => console.error('Помилка завантаження дошки:', err)
    });
  }

  selectBoard(boardId: number) {
    this.currentBoardId = boardId;

    const selectedBoard = this.boards.find(b => b.id == boardId);

    if (selectedBoard) {
      this.columns = selectedBoard.columns;
      this.columns.sort((a: any, b: any) => a.position - b.position);

      if (this.columns.length > 0) {
        this.newTask.columnId = this.columns[0].id;
      }
      this.cdr.detectChanges();
    }
  }

  onBoardChange(event: any) {
    const selectedId = Number.parseInt(event.target.value, 10);
    this.selectBoard(selectedId);
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

  openEditForm(task: any) {
    this.editingTaskId = task.id;
    this.newTask = { ...task };

    if (this.newTask.deadline) {
      this.newTask.deadline = new Date(this.newTask.deadline).toISOString().slice(0, 16);
    }
    this.showForm = true;
  }

  resetForm() {
    this.showForm = false;
    this.editingTaskId = null;

    const firstColumnId = this.columns.length > 0 ? this.columns[0].id : 0;
    this.newTask = { title: '', description: '', deadline: '', complexity: 5, columnId: firstColumnId };
  }

  saveTask() {
    if (!this.newTask.title) {
      alert('Будь ласка, введіть назву задачі!');
      return;
    }

    if (this.editingTaskId) {
      const updatedTask = { ...this.newTask, id: this.editingTaskId };
      this.http.put(`http://localhost:5133/api/tasks/${this.editingTaskId}`, updatedTask).subscribe({
        next: () => {
          this.fetchTasks();
          this.resetForm();
        },
        error: (err) => console.error('Помилка оновлення:', err)
      });
    } else {
      this.http.post<any>('http://localhost:5133/api/tasks', this.newTask).subscribe({
        next: () => {
          this.fetchTasks();
          this.resetForm();
        },
        error: (err) => console.error('Помилка створення:', err)
      });
    }
  }

  deleteTask(id: number) {
    if (confirm('Ви впевнені, що хочете видалити цю задачу?')) {
      this.http.delete(`http://localhost:5133/api/tasks/${id}`).subscribe({
        next: () => {
          this.fetchTasks();
        },
        error: (err) => {
          console.error('Помилка видалення:', err);
        }
      });
    }
  }

  moveTask(task: any, newColumnId: number) {
    const updatedTask = { ...task, columnId: newColumnId };

    this.http.put(`http://localhost:5133/api/tasks/${task.id}`, updatedTask).subscribe({
      next: () => {
        this.fetchTasks();
      },
      error: (err) => {
        console.error('Помилка оновлення задачі:', err);
      }
    });
  }

  // ==========================================
  // 💡 НОВЕ: Логіка для мишки (Спливаючий опис)
  // ==========================================

  onMouseEnter(task: any) {
    // Якщо в задачі немає опису, нам немає чого показувати
    if (!task.description || task.description.trim() === '') return;

    // Якщо мишка вже наведена, скидаємо старий таймер
    if (this.hoverTimeout) {
      clearTimeout(this.hoverTimeout);
    }

    // Запускаємо відлік на 2 секунди (2000 мс)
    this.hoverTimeout = setTimeout(() => {
      this.hoveredTaskId = task.id;
      this.cdr.detectChanges(); // Оновлюємо UI, щоб показати віконце
    }, 2000);
  }

  onMouseLeave() {
    // Коли мишка йде геть, скасовуємо таймер і ховаємо віконце
    if (this.hoverTimeout) {
      clearTimeout(this.hoverTimeout);
    }
    this.hoveredTaskId = null;
    this.cdr.detectChanges();
  }
  // ==========================================

  draggedTaskId: number | null = null;

  onDragStart(task: any) {
    this.draggedTaskId = task.id;
    this.onMouseLeave(); // 💡 Ховаємо підказку, якщо почали тягнути задачу
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
  }

  onDrop(event: DragEvent, newColumnId: number) {
    event.preventDefault();

    if (this.draggedTaskId) {
      const task = this.tasks.find(t => t.id === this.draggedTaskId);

      if (task && task.columnId !== newColumnId) {
        this.moveTask(task, newColumnId);
      }
      this.draggedTaskId = null;
    }
  }

  openShareModal() {
    if (!this.currentBoardId) {
      alert('Помилка: Дошка ще не завантажена!');
      return;
    }
    this.shareEmail = '';
    this.shareRole = 'Editor';
    this.showShareModal = true;
  }

  closeShareModal() {
    this.showShareModal = false;
  }

  submitShare() {
    if (!this.shareEmail.trim()) {
      alert('Будь ласка, введіть Email користувача!');
      return;
    }

    const requestBody = {
      boardId: this.currentBoardId,
      userEmail: this.shareEmail.trim(),
      accessLevel: this.shareRole
    };

    this.http.post('http://localhost:5133/api/Boards/share', requestBody).subscribe({
      next: (response: any) => {
        alert(response.message || 'Доступ успішно надано!');
        this.closeShareModal();
      },
      error: (err) => {
        console.error('Помилка спільного доступу:', err);
        alert(err.error?.message || 'Не вдалося надати доступ. Перевірте консоль.');
      }
    });
  }

  openAccessModal() {
    if (!this.currentBoardId) return;

    this.http.get<any[]>(`http://localhost:5133/api/Boards/${this.currentBoardId}/shared-users`).subscribe({
      next: (users) => {
        this.sharedUsers = users;
        this.showAccessModal = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Помилка завантаження користувачів:', err);
        alert('Не вдалося завантажити список доступу.');
      }
    });
  }

  closeAccessModal() {
    this.showAccessModal = false;
    this.sharedUsers = [];
    this.cdr.detectChanges();
  }

  revokeAccess(userId: number) {
    if (!confirm('Ви впевнені, що хочете забрати доступ у цього користувача? Він більше не побачить цю дошку.')) {
      return;
    }

    this.http.delete(`http://localhost:5133/api/Boards/${this.currentBoardId}/share/${userId}`).subscribe({
      next: () => {
        this.sharedUsers = this.sharedUsers.filter(u => u.id !== userId);
        this.fetchBoard();
      },
      error: (err) => {
        console.error('Помилка скасування доступу:', err);
        alert('Помилка при видаленні користувача.');
      }
    });
  }

  logout() {
    this.authService.logout();
  }
}
