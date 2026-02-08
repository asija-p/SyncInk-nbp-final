import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReplayTimeline } from './replay-timeline';

describe('ReplayTimeline', () => {
  let component: ReplayTimeline;
  let fixture: ComponentFixture<ReplayTimeline>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReplayTimeline]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ReplayTimeline);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
