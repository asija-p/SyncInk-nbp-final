import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { HomeBtn } from '../../shared/components/home-btn/home-btn';
import { AuthService, LoginDto } from '../auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-login',
  imports: [RouterLink, HomeBtn, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  username = '';
  password = '';

  constructor(
    private auth: AuthService,
    private router: Router,
  ) {}

  submitLogin() {
    const data: LoginDto = { username: this.username, password: this.password };

    this.auth.login(data).subscribe({
      next: () => {
        this.router.navigate(['/']); // go home after login
      },
      error: (err) => {
        console.error('Login failed', err);
      },
    });
  }
}
