import { Component, OnInit } from '@angular/core';
import { SignalrService } from './signalr.service';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  constructor(
    private connection: SignalrService,
    private auth: AuthService,
  ) {}

  ngOnInit() {
    this.connection.start();
  }
}
