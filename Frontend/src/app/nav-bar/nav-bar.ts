import { Component } from '@angular/core';
import { NavBtns } from '../nav-btns/nav-btns';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-nav-bar',
  imports: [NavBtns, RouterLink, CommonModule],
  templateUrl: './nav-bar.html',
  styleUrl: './nav-bar.scss',
})
export class NavBar {
  constructor(public route: Router) {}
}
