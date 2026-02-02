import { Component } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-nav-btns',
  imports: [CommonModule, RouterLink],
  templateUrl: './nav-btns.html',
  styleUrl: './nav-btns.scss',
})
export class NavBtns {
  constructor(
    public auth: AuthService,
    public route: Router,
  ) {}

  logout() {
    this.auth.logout().subscribe({
      next: () => {
        // Optional: Redirect to home or login after logout
        console.log('Logged out successfully');
      },
    });
  }
  goToProfile() {
    if (!this.auth.currentUser()) {
      this.route.navigate(['/login']);
      return;
    }

    this.route.navigate(['/profile']);
  }
}
