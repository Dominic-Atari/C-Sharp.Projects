import { Component, EventEmitter, Output } from '@angular/core';

import { FormsModule } from '@angular/forms';
import {
  IonButton,
  IonCard,
  IonCardContent,
  IonInput,
  IonItem,
  IonLabel,
  IonNote,
  IonTextarea,
} from '@ionic/angular/standalone';

export interface FeedPostPayload {
  author: string;
  message: string;
}

@Component({
    selector: 'app-feed-post-form',
    imports: [
    FormsModule,
    IonCard,
    IonCardContent,
    IonItem,
    IonInput,
    IonTextarea,
    IonButton,
    IonLabel,
    IonNote
],
    templateUrl: './feed-post-form.component.html',
    styleUrls: ['./feed-post-form.component.scss']
})
export class FeedPostFormComponent {
  @Output() create = new EventEmitter<FeedPostPayload>();

  author = '';
  message = '';

  submit(): void {
    const trimmedMessage = this.message.trim();
    const trimmedAuthor = this.author.trim();

    if (!trimmedMessage) {
      return;
    }

    this.create.emit({
      author: trimmedAuthor || 'Guest',
      message: trimmedMessage,
    });

    this.message = '';
  }
}
