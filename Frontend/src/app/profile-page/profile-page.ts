import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavBar } from '../nav-bar/nav-bar';
import { Router } from '@angular/router';
import { ProfileService } from './profile.service';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [NavBar, CommonModule],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
})
export class ProfilePage implements OnInit {
  savedPictures: any[] = [];
  loading: boolean = true;

  constructor(
    private profileService: ProfileService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.profileService.getSavedPictures().subscribe({
      next: (data) => {
        this.savedPictures = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load replays', err);
        this.loading = false;
      },
    });
  }

  initiateReplay(roomName: string, saveId: string) {
    this.profileService.startReplay(roomName, saveId).subscribe({
      next: () => {
        this.router.navigate(['/room', roomName], { queryParams: { replay: 'true' } });
      },
      error: (err) => {
        console.error('Replay start failed', err);

        // Better error message
        let message = 'Could not start replay';
        if (err.error) {
          if (typeof err.error === 'string') {
            message += ': ' + err.error;
          } else if (err.error.message) {
            message += ': ' + err.error.message;
          } else {
            message += ': ' + JSON.stringify(err.error);
          }
        }
        alert(message);
      },
    });
  }
}
