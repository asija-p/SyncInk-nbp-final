import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Injectable, signal } from '@angular/core'; // Added signal
import { UserProfile } from '../shared/interfaces/user.interface';
import { tap } from 'rxjs';
import { SignalrService } from '../signalr.service';

export interface LoginDto {
  username: string;
  password: string;
}

export interface RegisterDto {
  username: string;
  password: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly API_URL = environment.KEY_TO_READ;

  currentUser = signal<UserProfile | null>(null);
  isLoaded = signal<boolean>(false);

  constructor(
    private http: HttpClient,
    private signalr: SignalrService,
  ) {
    this.checkAuth().subscribe();
  }

  login(data: LoginDto) {
    return this.http
      .post<UserProfile>(`${this.API_URL}/auth/login`, data, { withCredentials: true })
      .pipe(
        tap((user) => {
          this.currentUser.set(user);
          this.signalr.start();
        }),
      );
  }

  register(data: RegisterDto) {
    return this.http
      .post<void>(`${this.API_URL}/auth/register`, data, { withCredentials: true })
      .pipe(
        tap(() => {
          this.signalr.start();
        }),
      );
  }

  checkAuth() {
    return this.http
      .get<UserProfile>(`${this.API_URL}/auth/check/me`, { withCredentials: true })
      .pipe(
        tap({
          next: (user) => {
            this.currentUser.set(user);
            this.isLoaded.set(true);
          },
          error: () => {
            this.currentUser.set(null);
            this.isLoaded.set(true);
          },
        }),
      );
  }

  logout() {
    return this.http
      .post<void>(`${this.API_URL}/auth/logout`, {}, { withCredentials: true })
      .pipe(tap(() => this.currentUser.set(null)));
  }
}
