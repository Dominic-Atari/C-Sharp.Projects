import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  IonContent,
  IonHeader,
  IonTitle,
  IonToolbar,
  IonCard,
  IonCardContent,
} from '@ionic/angular/standalone';

@Component({
  selector: 'app-user',
  standalone: true,
  imports: [CommonModule, IonHeader, IonToolbar, IonTitle, IonContent, IonCard, IonCardContent],
  templateUrl: './user.page.html',
  styleUrls: ['./user.page.scss'],
})
export class UserPage {
  today = new Date();
}
