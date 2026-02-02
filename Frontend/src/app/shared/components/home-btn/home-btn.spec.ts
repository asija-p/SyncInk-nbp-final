import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HomeBtn } from './home-btn';

describe('HomeBtn', () => {
  let component: HomeBtn;
  let fixture: ComponentFixture<HomeBtn>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomeBtn]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HomeBtn);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
