import { Component, ViewChild } from '@angular/core';
import { SignalrService } from '../signalr.service';
import { Canvas } from '../canvas/canvas';
import { BrushSettings } from '../brush-settings/brush-settings';
import { UsersList } from '../users-list/users-list';
import { ActivatedRoute } from '@angular/router';
import { ReplayTimeline, TimelineNode } from '../replay-timeline/replay-timeline';
import { ReplayService } from '../replay.service';
import { CommonModule } from '@angular/common';
@Component({
  selector: 'app-room',
  imports: [Canvas, BrushSettings, UsersList, ReplayTimeline, CommonModule],
  templateUrl: './room.html',
  styleUrls: ['./room.scss'],
})
export class Room {
  @ViewChild('canvasComp') canvasComponent!: Canvas;

  public isReplay = false;
  public timelineNodes: TimelineNode[] = [];

  constructor(
    private signalrService: SignalrService,
    private route: ActivatedRoute,
    private replayService: ReplayService,
  ) {
    const queryParams = this.route.snapshot.queryParamMap;
    this.isReplay = queryParams.get('replay') === 'true';
  }

  ngOnInit() {
    // Get roomName from the path
    const roomName = this.route.snapshot.paramMap.get('roomId'); // or 'roomName' depending on route
    if (!roomName) {
      console.error('Room name not found in URL');
      return;
    }

    if (this.route.snapshot.queryParamMap.get('replay') === 'true') {
      this.loadTimelineData(roomName);
    }
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

  loadTimelineData(roomName: string) {
    this.replayService.getTimelineData(roomName).subscribe({
      next: (data) => {
        this.timelineNodes = data;
      },
      error: (err) => {
        console.error('Failed to load timeline', err);
      },
    });
  }
}
