import { Component } from '@angular/core';
import { SignalrService } from '../signalr.service';
import { Footer } from '../footer/footer';
import { RouterOutlet } from '@angular/router';
import { NavBar } from '../nav-bar/nav-bar';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-home',
  imports: [Footer, NavBar, RouterOutlet],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  constructor(
    private connection: SignalrService,
    public auth: AuthService,
  ) {}

  MakeRoom() {
    this.connection.reqRoom();
  }
  JoinRoom(roomId: string) {
    this.connection.joinRoom(roomId);
  }
}
