import { Injectable } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import * as signalR from '@microsoft/signalr';
import { Stroke } from './models/stroke';
import { BehaviorSubject, Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private hubConnection!: signalR.HubConnection;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  private strokeSubject = new Subject<Stroke>();
  public receivedStrokes$ = this.strokeSubject.asObservable();

  private strokeRemovedSubject = new Subject<Stroke>();
  public strokeRemoved$ = this.strokeRemovedSubject.asObservable();

  private usersSubject = new BehaviorSubject<string[]>([]);
  public users$ = this.usersSubject.asObservable();

  private drawingStateSubject = new BehaviorSubject<{ [username: string]: boolean }>({});
  public drawingState$ = this.drawingStateSubject.asObservable();

  start() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5177/syncInkHub', { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveStroke', (stroke: Stroke) => {
      this.strokeSubject.next(stroke);
    });

    this.hubConnection.on('ReceiveReplayData', (strokes: Stroke[]) => {
      strokes.forEach((stroke) => this.strokeSubject.next(stroke));
    });

    this.hubConnection.on('StrokeRemoved', (stroke: Stroke) => {
      this.strokeRemovedSubject.next(stroke);
    });

    this.hubConnection.on('UsersUpdated', (users: string[]) => this.usersSubject.next(users));
    this.hubConnection.on('DrawingStateUpdated', (states) => this.drawingStateSubject.next(states));

    this.hubConnection.start().then(() => console.log('SignalR connected'));
  }

  // ------------------ Rooms ------------------

  async reqRoom() {
    const roomId = this.route.snapshot.paramMap.get('roomId');
    if (!roomId) {
      const newRoomId = await this.hubConnection.invoke<string>('MakeRoom');
      this.router.navigate(['room/', newRoomId]);
    }
  }

  async joinRoom(roomName: string) {
    const roomId = await this.hubConnection.invoke<string>('JoinRoom', roomName);
    this.router.navigate(['room/', roomId]);
  }

  async leaveRoom() {
    await this.hubConnection.invoke('LeaveRoom');
    this.router.navigate(['home']);
  }

  async receiveUsers() {
    this.hubConnection.on('UsersUpdated', (users: string[]) => {
      this.usersSubject.next(users);
    });
  }

  // ------------------ Strokes ------------------

  async getHistory(roomName: string) {
    if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('GetHistory', roomName);
    }
  }

  async getReplayHistory(roomName: string) {
    if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('GetReplayHistory', roomName);
    }
  }

  async sendStroke(stroke: Stroke, strokeId: string) {
    if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('SendStroke', stroke, strokeId);
    }
  }

  async completeStroke(stroke: Stroke, strokeId: string) {
    if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('CompleteStroke', stroke, strokeId);
    }
  }

  // ------------------ Drawing State ------------------

  async setDrawing(isDrawing: boolean) {
    await this.hubConnection.invoke('SetDrawing', isDrawing);
  }

  async receiveDrawingState() {
    this.hubConnection.on('DrawingStateUpdated', (states: { [username: string]: boolean }) => {
      this.drawingStateSubject.next(states);
    });
  }

  // ------------------ Undo / Redo ------------------

  async undoStroke() {
    if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('UndoStroke');
    }
  }

  async redoStroke() {
    if (this.hubConnection.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('RedoStroke');
    }
  }
}
