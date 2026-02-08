import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { Subscription, combineLatest } from 'rxjs';
import { SignalrService } from '../signalr.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-users-list',
  imports: [CommonModule],
  templateUrl: './users-list.html',
  styleUrls: ['./users-list.scss'], // fixed typo: styleUrl -> styleUrls
})
export class UsersList implements OnInit, OnDestroy {
  @Input() isReplay: boolean = false; // <-- added input to receive flag from parent

  users: string[] = [];
  drawingStates: { [username: string]: boolean } = {};
  private sub!: Subscription;

  constructor(private signalr: SignalrService) {}

  ngOnInit(): void {
    this.sub = combineLatest([this.signalr.users$, this.signalr.drawingState$]).subscribe(
      ([users, drawingStates]) => {
        this.users = users;
        this.drawingStates = drawingStates;
      },
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
