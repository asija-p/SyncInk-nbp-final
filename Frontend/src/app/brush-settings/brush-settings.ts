import { Component } from '@angular/core';
import { Store } from '@ngrx/store';
import { ColorPickerDirective } from 'ngx-color-picker';
import { debounce, debounceTime, Subject } from 'rxjs';
import { setColor, setSize } from '../store/syncink.actions';
import { selectBrushColor, selectBrushSize } from '../store/syncink.selectors';
import { MatSliderModule } from '@angular/material/slider';
import { FormsModule } from '@angular/forms';
import { BrushSettingsService } from './brush-settings.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-brush-settings',
  imports: [ColorPickerDirective, MatSliderModule, FormsModule],
  templateUrl: './brush-settings.html',
  styleUrl: './brush-settings.scss',
})
export class BrushSettings {
  private colorSubject = new Subject<string>();
  private sizeSubject = new Subject<number>();
  color: string;
  size: number;
  public isReplay = false;
  constructor(
    private store: Store,
    private route: ActivatedRoute,
    private brushSettingsService: BrushSettingsService,
  ) {
    const queryParams = this.route.snapshot.queryParamMap;
    this.isReplay = queryParams.get('replay') === 'true';
    this.store.select(selectBrushColor).subscribe((value) => {
      this.color = value;
    });

    this.store.select(selectBrushSize).subscribe((value) => {
      this.size = value;
    });

    this.colorSubject.pipe(debounceTime(200)).subscribe((value) => {
      this.store.dispatch(setColor({ color: value }));
    });

    this.sizeSubject.pipe(debounceTime(200)).subscribe((value) => {
      this.store.dispatch(setSize({ size: value }));
    });
  }
  cassandraSave() {
    this.brushSettingsService.cassandraSave();
  }

  onColorChange() {
    this.colorSubject.next(this.color);
  }

  onSizeChange() {
    this.sizeSubject.next(this.size);
  }
}
