import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { HomeBtn } from '../../shared/components/home-btn/home-btn';
import { AuthService, RegisterDto } from '../auth.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-register',
  imports: [RouterLink, FormsModule, HomeBtn],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  username = '';
  password = '';
  confirmPassword = '';

  constructor(
    private auth: AuthService,
    private router: Router,
  ) {}

  submitRegister() {
    if (this.password !== this.confirmPassword) {
      console.error('Passwords do not match');
      return;
    }

    const data: RegisterDto = {
      username: this.username,
      password: this.password,
    };

    this.auth.register(data).subscribe({
      next: () => {
        // optional: auto-login flow could go here
        this.router.navigate(['/home']);
      },
      error: (err) => {
        console.error('Registration failed', err);
      },
    });
  }
}
