import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NavBtns } from './nav-btns';

describe('NavBtns', () => {
  let component: NavBtns;
  let fixture: ComponentFixture<NavBtns>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavBtns]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NavBtns);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
