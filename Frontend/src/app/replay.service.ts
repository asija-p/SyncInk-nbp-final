import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { TimelineNode } from './replay-timeline/replay-timeline';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ReplayService {
  private readonly API_URL = environment.KEY_TO_READ;
  constructor(private http: HttpClient) {}

  getTimelineData(roomName: string): Observable<TimelineNode[]> {
    return this.http.get<TimelineNode[]>(
      `${this.API_URL}/cassandra/replay-timeline?roomName=${roomName}`,
    );
  }
}
