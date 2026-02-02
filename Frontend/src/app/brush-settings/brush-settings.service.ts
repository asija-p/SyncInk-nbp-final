import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class BrushSettingsService {
  private readonly API_URL = environment.KEY_TO_READ;
  constructor(private http: HttpClient) {}
  async cassandraSave() {
    const path = window.location.pathname;

    const segments = path.split('/');
    const roomName = segments[segments.length - 1];

    return this.http
      .post<void>(
        `${this.API_URL}/cassandra/save?roomName=${roomName}`,
        {},
        { withCredentials: true },
      )
      .toPromise();
  }
}
