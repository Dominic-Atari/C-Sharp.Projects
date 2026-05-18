import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { IonButton, IonContent, IonHeader, IonInput, IonItem, IonLabel, IonToolbar, IonTitle, IonIcon } from '@ionic/angular/standalone';

@Component({
  selector: 'app-record-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, IonHeader, IonToolbar, IonTitle, IonContent, IonItem, IonLabel, IonInput, IonButton, IonIcon],
  templateUrl: './record-modal.component.html',
  styleUrls: ['./record-modal.component.scss'],
})
export class RecordModalComponent {
  showEdit = false;
  subjectName = '';

  constructor(private modalCtrl: ModalController) {}

  toggleEdit(): void {
    this.showEdit = !this.showEdit;
  }

  // Preview/action pill editable labels
  reactionsLabel = '0';
  remixesLabel = '0';
  reactLabel = 'React';
  remixLabel = 'Remix';

  // edit toggles for each pill
  editReactions = false;
  editRemixes = false;
  editReact = false;
  editRemix = false;

  toggleEditPill(pill: 'reactions' | 'remixes' | 'react' | 'remix'): void {
    switch (pill) {
      case 'reactions':
        this.editReactions = !this.editReactions; break;
      case 'remixes':
        this.editRemixes = !this.editRemixes; break;
      case 'react':
        this.editReact = !this.editReact; break;
      case 'remix':
        this.editRemix = !this.editRemix; break;
    }
  }

  async done(): Promise<void> {
    await this.modalCtrl.dismiss({ subjectName: this.subjectName ?? '' });
  }

  async cancel(): Promise<void> {
    await this.modalCtrl.dismiss();
  }
}

