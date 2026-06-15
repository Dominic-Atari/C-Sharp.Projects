import { Component, OnInit } from '@angular/core';
import { IonApp, IonRouterOutlet } from '@ionic/angular/standalone';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [IonApp, IonRouterOutlet],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  constructor(private router: Router) {}

  ngOnInit(): void {
    try {
      const auth = localStorage.getItem('nile.auth');
      if (!auth) {
        // No saved auth — send the user to the login page on first load
        this.router.navigateByUrl('/auth?mode=login');
      }
    } catch (err) {
      // If localStorage is unavailable for any reason, still navigate to auth
      this.router.navigateByUrl('/auth?mode=login');
    }
  }
}
