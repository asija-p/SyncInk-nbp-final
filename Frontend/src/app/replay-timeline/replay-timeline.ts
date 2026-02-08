import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

export interface TimelineNode {
  minuteBucket: string;
  strokesCompleted: number;
  undos: number;
  redos: number;
  activeUsers: string[];
}

@Component({
  selector: 'app-replay-timeline',
  imports: [CommonModule],
  templateUrl: './replay-timeline.html',
  styleUrl: './replay-timeline.scss',
})
export class ReplayTimeline {
  @Input() nodes: TimelineNode[] = [];
}
