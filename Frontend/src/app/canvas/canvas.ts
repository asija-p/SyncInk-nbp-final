import { AfterViewInit, Component, ElementRef, Input, ViewChild } from '@angular/core';
import { fromEvent, pairwise, switchMap, takeUntil, tap, finalize } from 'rxjs';

import { CommonModule } from '@angular/common';
import { Position, Stroke } from '../models/stroke';
import { Store } from '@ngrx/store';
import { selectBrushColor, selectBrushSize } from '../store/syncink.selectors';
import { ActivatedRoute } from '@angular/router';
import { SignalrService } from '../signalr.service';

@Component({
  selector: 'app-canvas',
  imports: [CommonModule],
  templateUrl: './canvas.html',
  styleUrls: ['./canvas.scss'],
})
export class Canvas implements AfterViewInit {
  @ViewChild('canvas') public canvas!: ElementRef<HTMLCanvasElement>;
  @Input() public width = 700;
  @Input() public height = 700;

  private cx!: CanvasRenderingContext2D;
  private myBrushColor = '#000';
  private myBrushSize = 5;
  private strokes: Stroke[] = [];
  private undoneStrokes: Stroke[] = [];

  constructor(
    private connection: SignalrService,
    private store: Store,
    private route: ActivatedRoute,
  ) {
    this.store.select(selectBrushColor).subscribe((value) => {
      this.myBrushColor = value;
    });
    this.store.select(selectBrushSize).subscribe((value) => {
      this.myBrushSize = value;
    });
  }

  ngAfterViewInit(): void {
    const canvasEl = this.canvas.nativeElement;

    this.cx = canvasEl.getContext('2d')!;
    canvasEl.width = this.width;
    canvasEl.height = this.height;

    this.cx.fillStyle = '#fff';
    this.cx.fillRect(0, 0, canvasEl.width, canvasEl.height);
    this.cx.lineCap = 'round';

    this.connection.start();

    // Check if replay mode is active
    const queryParams = this.route.snapshot.queryParamMap;
    const isReplay = queryParams.get('replay') === 'true';

    if (!isReplay) {
      this.captureEvents(canvasEl);
    }

    this.connection.receivedStrokes$.subscribe((stroke) => this.handleStrokeAdded(stroke));
    this.connection.strokeRemoved$.subscribe((stroke) => this.handleStrokeRemoved(stroke));

    const roomId = this.route.snapshot.paramMap.get('roomId');
    if (roomId) {
      setTimeout(() => this.connection.getHistory(roomId), 200);
    }
  }

  private captureEvents(canvasEl: HTMLCanvasElement) {
    fromEvent<MouseEvent>(canvasEl, 'mousedown')
      .pipe(
        switchMap(() => {
          const strokeId = crypto.randomUUID();
          const points: Position[] = [];
          this.connection.setDrawing(true);

          const mouseMove$ = fromEvent<MouseEvent>(canvasEl, 'mousemove').pipe(
            takeUntil(fromEvent<MouseEvent>(canvasEl, 'mouseup')),
            takeUntil(fromEvent<MouseEvent>(canvasEl, 'mouseleave')),
          );

          return mouseMove$.pipe(
            pairwise(),
            tap(([prev, curr]) => {
              const rect = canvasEl.getBoundingClientRect();
              const prevPos = { x: prev.clientX - rect.left, y: prev.clientY - rect.top };
              const currPos = { x: curr.clientX - rect.left, y: curr.clientY - rect.top };

              points.push(currPos);

              this.drawSegment(prevPos, currPos);

              const miniStroke: Stroke = {
                id: strokeId,
                points: [currPos],
                color: this.myBrushColor,
                size: this.myBrushSize,
                visible: true,
                strokeDate: new Date().toISOString(),
              };
              this.connection.sendStroke(miniStroke, strokeId);
            }),
            finalize(() => {
              if (points.length > 0) {
                const fullStroke: Stroke = {
                  id: strokeId,
                  points,
                  color: this.myBrushColor,
                  size: this.myBrushSize,
                  visible: true,
                  strokeDate: new Date().toISOString(),
                };
                this.connection.completeStroke(fullStroke, strokeId);
              }
              this.connection.setDrawing(false);
            }),
          );
        }),
      )
      .subscribe();
  }

  private handleStrokeAdded(stroke: Stroke) {
    this.undoneStrokes = this.undoneStrokes.filter((s) => s.id !== stroke.id);

    const existing = this.strokes.find((s) => s.id === stroke.id);
    if (existing) {
      if (stroke.points.length > 1) {
        existing.points = stroke.points;
      } else {
        this.mergeStrokePoints(existing, stroke);
      }
      existing.visible = stroke.visible ?? true;
    } else {
      this.strokes.push(stroke);
    }

    this.redrawCanvas();
  }

  private mergeStrokePoints(existing: Stroke, newStroke: Stroke) {
    if (!existing.points.length) {
      existing.points.push(...newStroke.points);
      return;
    }

    const last = existing.points[existing.points.length - 1];
    const firstNew = newStroke.points[0];

    const pointsToAdd = [...newStroke.points];

    if (last.x === firstNew.x && last.y === firstNew.y) {
      pointsToAdd.shift();
    }

    existing.points.push(...pointsToAdd);
  }

  private handleStrokeRemoved(stroke: Stroke) {
    const existing = this.strokes.find((s) => s.id === stroke.id);
    if (existing) {
      existing.visible = false;
      this.undoneStrokes.push(existing);
    }
    this.redrawCanvas();
  }

  public undo() {
    if (!this.strokes.length) return;
    this.connection.undoStroke();
  }

  public redo() {
    if (!this.undoneStrokes.length) return;
    this.connection.redoStroke();
  }

  private redrawCanvas() {
    const canvasEl = this.canvas.nativeElement;
    this.cx.clearRect(0, 0, canvasEl.width, canvasEl.height);

    this.cx.fillStyle = '#fff';
    this.cx.fillRect(0, 0, canvasEl.width, canvasEl.height);

    for (let stroke of this.strokes) {
      if (stroke.visible !== false) {
        this.drawStroke(stroke);
      }
    }
  }

  private drawSegment(prev: Position, cur: Position) {
    if (!this.cx) return;

    this.cx.strokeStyle = this.myBrushColor;
    this.cx.lineWidth = this.myBrushSize;
    this.cx.lineCap = 'round';
    this.cx.lineJoin = 'round';

    this.cx.beginPath();
    this.cx.moveTo(prev.x, prev.y);
    this.cx.lineTo(cur.x, cur.y);
    this.cx.stroke();
  }

  private drawStroke(stroke: Stroke) {
    if (!stroke.points.length || !this.cx) return;

    this.cx.strokeStyle = stroke.color;
    this.cx.lineWidth = stroke.size;
    this.cx.lineCap = 'round';
    this.cx.lineJoin = 'round';

    if (stroke.points.length === 1) {
      this.cx.beginPath();
      this.cx.arc(stroke.points[0].x, stroke.points[0].y, stroke.size / 2, 0, Math.PI * 2);
      this.cx.fillStyle = stroke.color;
      this.cx.fill();
    } else if (stroke.points.length === 2) {
      this.cx.beginPath();
      this.cx.moveTo(stroke.points[0].x, stroke.points[0].y);
      this.cx.lineTo(stroke.points[1].x, stroke.points[1].y);
      this.cx.stroke();
    } else {
      this.drawSmoothCurve(stroke.points, stroke.size, stroke.color);
    }
  }

  private drawSmoothCurve(points: Position[], lineWidth: number, color: string) {
    if (points.length < 2) return;

    this.cx.strokeStyle = color;
    this.cx.lineWidth = lineWidth;
    this.cx.lineCap = 'round';
    this.cx.lineJoin = 'round';

    this.cx.beginPath();
    this.cx.moveTo(points[0].x, points[0].y);

    for (let i = 1; i < points.length - 2; i++) {
      const xc = (points[i].x + points[i + 1].x) / 2;
      const yc = (points[i].y + points[i + 1].y) / 2;
      this.cx.quadraticCurveTo(points[i].x, points[i].y, xc, yc);
    }

    if (points.length > 2) {
      const n = points.length - 1;
      this.cx.quadraticCurveTo(points[n - 1].x, points[n - 1].y, points[n].x, points[n].y);
    } else {
      this.cx.lineTo(points[1].x, points[1].y);
    }

    this.cx.stroke();
  }
}
