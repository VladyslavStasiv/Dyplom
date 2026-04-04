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

  hoverTimeout: any;
  hoveredTaskId: number | null = null;

  isDarkMode: boolean = false;

  invitations: any[] = [];
  showInvitationsModal = false;

  // ==========================================
  // 💡 ВЛАСНІ ВІКОНЦЯ (Оновлено для миттєвого відображення)
  // ==========================================
  customAlertMessage: string | null = null;
  customConfirmMessage: string | null = null;
  confirmAction: (() => void) | null = null;

  showAlert(message: string) {
    this.customAlertMessage = message;
    this.cdr.detectChanges(); // 💡 ПРИМУСОВЕ оновлення екрану!
  }

  closeAlert() {
    this.customAlertMessage = null;
    this.cdr.detectChanges();
  }

  showConfirm(message: string, action: () => void) {
    this.customConfirmMessage = message;
    this.confirmAction = action;
    this.cdr.detectChanges();
  }

  confirmYes() {
    if (this.confirmAction) {
      this.confirmAction();
    }
    this.closeConfirm();
  }

  confirmNo() {
    this.closeConfirm();
  }

  closeConfirm() {
    this.customConfirmMessage = null;
    this.confirmAction = null;
    this.cdr.detectChanges();
  }
  // ==========================================

  fetchInvitations() {
    this.http.get<any[]>('http://localhost:5133/api/boards/invitations').subscribe({
      next: (data) => {
        this.invitations = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Помилка завантаження запрошень:', err)
    });
  }

  acceptInvitation(boardId: number) {
    this.http.post(`http://localhost:5133/api/boards/${boardId}/accept`, {}).subscribe({
      next: () => {
        this.showAlert('Запрошення успішно прийнято!');
        this.fetchInvitations();
        this.fetchBoard();
        if (this.invitations.length <= 1) this.closeInvitationsModal();
      },
      error: (err) => console.error('Помилка прийняття:', err)
    });
  }

  declineInvitation(boardId: number) {
    this.showConfirm('Ви впевнені, що хочете відхилити це запрошення?', () => {
      this.http.post(`http://localhost:5133/api/boards/${boardId}/decline`, {}).subscribe({
        next: () => {
          this.fetchInvitations();
          if (this.invitations.length <= 1) this.closeInvitationsModal();
        },
        error: (err) => console.error('Помилка відхилення:', err)
      });
    });
  }

  openInvitationsModal() {
    if (this.invitations.length > 0) {
      this.showInvitationsModal = true;
    } else {
      this.showAlert('У вас немає нових запрошень.');
    }
  }

  closeInvitationsModal() {
    this.showInvitationsModal = false;
  }

  toggleDarkMode() {
    this.isDarkMode = !this.isDarkMode;
    localStorage.setItem('theme', this.isDarkMode ? 'dark' : 'light');
  }

  exportToCSV() {
    if (this.filteredTasks.length === 0) {
      this.showAlert('Немає задач для експорту на цій дошці!');
      return;
    }

    let csvContent = "Назва;Опис;Дедлайн;Складність;Статус\n";

    this.filteredTasks.forEach(t => {
      const column = this.columns.find(c => c.id == t.columnId);
      const columnName = column ? column.title : "Невідомо";

      // 💡 ВІДШЛІФОВАНО: Жорстко задаємо український формат дати для таблиці
      let deadline = "Ні";
      if (t.deadline) {
        const d = new Date(t.deadline);
        deadline = d.toLocaleString('uk-UA', {
          day: '2-digit', month: '2-digit', year: 'numeric',
          hour: '2-digit', minute: '2-digit'
        });
      }

      const title = String(t.title || "").replaceAll(";", ",");
      const desc = String(t.description || "")
                    .replaceAll(";", ",")
                    .replaceAll(/[\n\r]/g, " ");

      csvContent += `${title};${desc};${deadline};${t.complexity};${columnName}\n`;
    });

    const blob = new Blob(["\uFEFF" + csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement("a");
    const url = URL.createObjectURL(blob);

    // 💡 ВІДШЛІФОВАНО: Форматуємо дату в самій назві файлу
    const fileNameDate = new Date().toLocaleDateString('uk-UA');
    link.setAttribute("href", url);
    link.setAttribute("download", `Звіт_задач_${fileNameDate}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    link.remove();
  }

  // ==========================================
  // 🎯 ШВИДКІ ФІЛЬТРИ-ЧІПСИ
  // ==========================================
  activeFilter: string = 'all';

  setFilter(filter: string) {
    this.activeFilter = filter;
    this.cdr.detectChanges();
  }

  get filteredTasks() {
    let result = this.tasks;
    const doneColumnId = this.columns.length > 2 ? this.columns[2].id : -1;

    // 1. Фільтруємо за активним чіпсом + ігноруємо колонку "Готово"
    if (this.activeFilter === 'critical') {
      result = result.filter(t => t.priorityScore >= 70 && t.columnId !== doneColumnId);
    } else if (this.activeFilter === 'high') {
      result = result.filter(t => t.priorityScore >= 40 && t.priorityScore < 70 && t.columnId !== doneColumnId);
    } else if (this.activeFilter === 'overdue') {
      result = result.filter(t => this.isOverdue(t) && t.columnId !== doneColumnId);
    }

    // 2. Пошук за текстом (залишаємо як є)
    if (this.searchQuery && this.searchQuery.trim() !== '') {
      const lowerQuery = this.searchQuery.toLowerCase();
      result = result.filter(t => {
        const safeTitle = String(t.title || '').toLowerCase();
        const safeDesc = String(t.description || '').toLowerCase();
        return safeTitle.includes(lowerQuery) || safeDesc.includes(lowerQuery);
      });
    }

    return result;
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

  isOverdue(task: any): boolean {
    if (!task.deadline) return false;

    if (this.columns.length > 2 && task.columnId === this.columns[2].id) {
      return false;
    }

    const deadlineDate = new Date(task.deadline).getTime();
    const now = Date.now();

    return deadlineDate < now;
  }

  getPriorityConfig(score: number) {
    if (score >= 70) return { icon: '🔥', color: '#d32f2f', text: 'Критичний' };
    if (score >= 40) return { icon: '🔴', color: '#e67e22', text: 'Високий' };
    if (score >= 20) return { icon: '🟡', color: '#f1c40f', text: 'Середній' };
    return { icon: '🟢', color: '#2ecc71', text: 'Низький' };
  }

  // ==========================================
  // 💡 РЕКОМЕНДАЦІЙНА СИСТЕМА
  // ==========================================
  recommendedTaskId: number | null = null;

  recommendNextTask() {
    if (this.columns.length < 2) return;

    const activeColumnIds = new Set([this.columns[0].id, this.columns[1].id]);
    const activeTasks = this.tasks.filter(t => activeColumnIds.has(t.columnId));

    if (activeTasks.length === 0) {
      this.showAlert('У вас немає активних задач. Ви вільні! 🎉');
      return;
    }

    let nextTask = activeTasks[0];
    for (const task of activeTasks) {
      if (task.priorityScore > nextTask.priorityScore) {
        nextTask = task;
      }
    }

    this.recommendedTaskId = nextTask.id;
    this.cdr.detectChanges();

    setTimeout(() => {
      const element = document.getElementById('task-' + nextTask.id);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    }, 100);

    setTimeout(() => {
      this.recommendedTaskId = null;
      this.cdr.detectChanges();
    }, 5000);
  }

  // ==========================================
  // 📜 ІСТОРІЯ ДІЙ (AUDIT LOG)
  // ==========================================
  boardHistory: any[] = [];
  showHistoryModal = false;

  openHistoryModal() {
    if (!this.currentBoardId) return;

    this.http.get<any[]>(`http://localhost:5133/api/tasks/history/${this.currentBoardId}`).subscribe({
      next: (data) => {
        this.boardHistory = data;
        this.showHistoryModal = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Помилка завантаження історії:', err);
        this.showAlert('Не вдалося завантажити історію дій.');
      }
    });
  }

  closeHistoryModal() {
    this.showHistoryModal = false;
  }

  // ==========================================
  // 🎉 МІКРО-АНІМАЦІЯ: КОНФЕТТІ
  // ==========================================
  activeConfetti: any[] = [];

  shootConfetti() {
    const colors = ['#f1c40f', '#e74c3c', '#9b59b6', '#3498db', '#2ecc71', '#e67e22', '#1abc9c'];
    const newConfetti = [];

    // Генеруємо 70 частинок конфетті з випадковими параметрами
    for (let i = 0; i < 70; i++) {
      newConfetti.push({
        left: Math.random() * 100 + 'vw', // Випадкова позиція по горизонталі
        color: colors[Math.floor(Math.random() * colors.length)], // Випадковий колір
        duration: (Math.random() * 2 + 2) + 's', // Швидкість падіння (від 2 до 4 секунд)
        delay: Math.random() * 0.5 + 's', // Затримка старту
        width: (Math.random() * 8 + 6) + 'px',
        height: (Math.random() * 12 + 10) + 'px'
      });
    }

    this.activeConfetti = newConfetti;

    // Прибираємо сміття з екрану через 4 секунди
    setTimeout(() => {
      this.activeConfetti = [];
      this.cdr.detectChanges();
    }, 4000);
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
    const savedTheme = localStorage.getItem('theme');
    this.isDarkMode = savedTheme === 'dark';

    this.username = this.authService.getUsername();
    this.fetchBoard();
    this.fetchTasks();
    this.fetchInvitations();
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
      this.showAlert('Будь ласка, введіть назву задачі!');
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
    this.showConfirm('Ви впевнені, що хочете видалити цю задачу?', () => {
      this.http.delete(`http://localhost:5133/api/tasks/${id}`).subscribe({
        next: () => {
          this.fetchTasks();
        },
        error: (err) => {
          console.error('Помилка видалення:', err);
        }
      });
    });
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

  onMouseEnter(task: any) {
    if (!task.description || task.description.trim() === '') return;
    if (this.hoverTimeout) clearTimeout(this.hoverTimeout);

    this.hoverTimeout = setTimeout(() => {
      this.hoveredTaskId = task.id;
      this.cdr.detectChanges();
    }, 2000);
  }

  onMouseLeave() {
    if (this.hoverTimeout) clearTimeout(this.hoverTimeout);
    this.hoveredTaskId = null;
    this.cdr.detectChanges();
  }

  draggedTaskId: number | null = null;

  onDragStart(task: any) {
    this.draggedTaskId = task.id;
    this.onMouseLeave();
  }

 onDragOver(event: DragEvent) {
    event.preventDefault(); // Дозволяє "кинути" елемент

    const wrapper = document.querySelector('.columns-wrapper');
    if (wrapper) {
      const touchX = event.clientX;
      const screenWidth = window.innerWidth;
      const threshold = 100; // Зона в 100px від країв

      // Захист від нульових координат
      if (touchX === 0) return;

      // 💡 Безпечний скрол: пересуваємо екран на 25px кожного разу,
      // коли палець або мишка "совається" в цій зоні
      if (touchX > screenWidth - threshold) {
        wrapper.scrollLeft += 25;
      }
      else if (touchX < threshold) {
        wrapper.scrollLeft -= 25;
      }
    }
  }

  onDrop(event: DragEvent, newColumnId: number) {
    event.preventDefault();

    if (this.draggedTaskId) {
      const task = this.tasks.find(t => t.id === this.draggedTaskId);

      if (task && task.columnId !== newColumnId) {
        this.moveTask(task, newColumnId);

        if (this.columns.length > 2 && newColumnId === this.columns[2].id) {
          this.shootConfetti();
        }
      }
      this.draggedTaskId = null;
    }
  }

  openShareModal() {
    if (!this.currentBoardId) {
      this.showAlert('Помилка: Дошка ще не завантажена!');
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
      this.showAlert('Будь ласка, введіть Email користувача!');
      return;
    }

    const requestBody = {
      boardId: this.currentBoardId,
      userEmail: this.shareEmail.trim(),
      accessLevel: this.shareRole
    };

    this.http.post('http://localhost:5133/api/Boards/share', requestBody).subscribe({
      next: (response: any) => {
        this.showAlert(response.message || 'Запрошення успішно надіслано!');
        this.closeShareModal();
      },
      error: (err) => {
        console.error('Помилка спільного доступу:', err);
        this.showAlert(err.error?.message || 'Не вдалося надіслати запрошення.');
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
        this.showAlert('Не вдалося завантажити список доступу.');
      }
    });
  }

  closeAccessModal() {
    this.showAccessModal = false;
    this.sharedUsers = [];
    this.cdr.detectChanges();
  }

  revokeAccess(userId: number) {
    this.showConfirm('Ви впевнені, що хочете забрати доступ у цього користувача (або скасувати запрошення)?', () => {
      this.http.delete(`http://localhost:5133/api/Boards/${this.currentBoardId}/share/${userId}`).subscribe({
        next: () => {
          this.sharedUsers = this.sharedUsers.filter(u => u.id !== userId);
          this.fetchBoard();
        },
        error: (err) => {
          console.error('Помилка скасування доступу:', err);
          this.showAlert('Помилка при видаленні користувача.');
        }
      });
    });
  }

  logout() {
    this.authService.logout();
  }
}
