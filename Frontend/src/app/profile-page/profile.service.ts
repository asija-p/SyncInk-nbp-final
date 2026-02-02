import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly API_URL = environment.KEY_TO_READ;

  constructor(private http: HttpClient) {}

  getSavedPictures(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/replay/list`, { withCredentials: true });
  }

  startReplay(roomName: string, saveId: string): Observable<any> {
    return this.http.post(
      `${this.API_URL}/replay/start`,
      { roomName, saveId },
      { withCredentials: true },
    );
  }
}
