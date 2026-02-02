import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BrushSettings } from './brush-settings';

describe('BrushSettings', () => {
  let component: BrushSettings;
  let fixture: ComponentFixture<BrushSettings>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BrushSettings]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BrushSettings);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
