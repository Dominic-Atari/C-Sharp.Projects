import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, PopoverController } from '@ionic/angular';
import { Router } from '@angular/router';
import { CreateSubjectPage } from '../create-subject/create-subject.page';

@Component({
  selector: 'app-school-logo-popover',
  standalone: true,
  imports: [CommonModule, IonicModule],
  template: `
    <div style="padding:8px 12px;min-width:200px">
      <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:8px">
        <div style="font-weight:700">{{ schoolName || 'School' }}</div>
        <div style="font-size:12px;color:var(--ion-color-medium)">ID: {{ schoolId || '—' }}</div>
      </div>

      <div style="display:flex;flex-direction:column;gap:8px;margin-top:6px">
        <ion-button size="small" expand="block" (click)="goToEdit()">Edit school</ion-button>
        <ion-button size="small" expand="block" (click)="openCreateSubject()">Create subject</ion-button>
      </div>
    </div>
  `,
})
export class SchoolLogoPopoverComponent {
  @Input() schoolId?: string | null;
  @Input() schoolName?: string | null;

  constructor(private router: Router, private popCtrl: PopoverController) {}

  goToEdit(): void {
    this.router.navigateByUrl('/school/edit');
    void this.popCtrl.dismiss();
  }

  async openCreateSubject(): Promise<void> {
    try { await this.popCtrl.dismiss(); } catch {}
    const p = await this.popCtrl.create({ component: CreateSubjectPage, translucent: true });
    await p.present();
  }
}
