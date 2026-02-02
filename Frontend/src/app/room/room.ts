import { Component, ViewChild } from '@angular/core';
import { SignalrService } from '../signalr.service';
import { Canvas } from '../canvas/canvas';
import { BrushSettings } from '../brush-settings/brush-settings';
import { UsersList } from '../users-list/users-list';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-room',
  imports: [Canvas, BrushSettings, UsersList],
  templateUrl: './room.html',
  styleUrls: ['./room.scss'], // fixed typo from styleUrl -> styleUrls
})
export class Room {
  @ViewChild('canvasComp') canvasComponent!: Canvas;
  public isReplay = false;

  constructor(
    private signalrService: SignalrService,
    private route: ActivatedRoute,
  ) {
    const queryParams = this.route.snapshot.queryParamMap;
    this.isReplay = queryParams.get('replay') === 'true';
  }

  async LeaveRoom() {
    this.signalrService.leaveRoom();
  }

  undo() {
    if (this.canvasComponent && !this.isReplay) {
      this.canvasComponent.undo();
    }
  }

  redo() {
    if (this.canvasComponent && !this.isReplay) {
      this.canvasComponent.redo();
    }
  }
}
