import { Component, Input, OnChanges } from '@angular/core';
import { IonicModule, ModalController } from '@ionic/angular';

import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
    selector: 'app-student-list-modal',
    imports: [IonicModule, FormsModule],
    template: `
    <ion-header>
      <ion-toolbar>
        <ion-title>{{ (students?.length ?? 0) }} {{ (students?.length ?? 0) === 1 ? 'learner' : 'learners' }}</ion-title>
        <ion-buttons slot="start">
          <ion-button fill="clear" size="small" title="Search" (click)="toggleSearch()">
            <ion-icon name="search-outline"></ion-icon>
            <span style="margin-left:6px">Search</span>
          </ion-button>
        </ion-buttons>
        <ion-buttons slot="end">
          <ion-button (click)="close()">Close</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <!-- learners preview + optional search -->
      <div style="padding:12px">
        @if (showSearch) {
          <ion-searchbar placeholder="Search learners" [(ngModel)]="query" (ionInput)="onSearch($event)"></ion-searchbar>
        }
        <!-- Show quick list of learner names below the search bar -->
        <div style="padding:0 0 12px 0;">
          <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center">
            <!-- stable preview of the first N learners -->
            @for (s of preview; track s) {
              <ion-chip (click)="openPerformance(s)" style="cursor:pointer">
                <ion-label>{{ displayName(s) }}</ion-label>
              </ion-chip>
            }
            @if ((students?.length ?? 0) > defaultDisplayLimit) {
              <small style="align-self:center;color:var(--ion-color-medium);">+{{ (students?.length ?? 0) - defaultDisplayLimit }} more</small>
            }
          </div>
        </div>
      </div>
      <ion-list>
        @for (s of display; track s) {
          <ion-item>
            <ion-label (click)="openPerformance(s)" style="cursor:pointer">{{ displayName(s) }}</ion-label>
            <ion-button slot="end" fill="clear" (click)="openPerformance(s)">View</ion-button>
          </ion-item>
        }
        @if (!students || students.length === 0) {
          <ion-item>No learners found</ion-item>
        }
      </ion-list>
    </ion-content>
    `
})
export class StudentListModalComponent implements OnChanges {
  @Input() students?: Array<any> | null;
  @Input() schoolId?: string | null;
  filtered: Array<any> = [];
  display: Array<any> = [];
  preview: Array<any> = [];
  query = '';
  // Show the search bar by default for discoverability
  showSearch = true;
  readonly defaultDisplayLimit = 10;

  constructor(private modalCtrl: ModalController, private router: Router) {}

  ngOnChanges(): void {
    // Reset filter when students change
    this.filtered = (this.students || []).slice();
    this.preview = (this.filtered || []).slice(0, this.defaultDisplayLimit);
    this.computeDisplay();
  }

  

  close() { this.modalCtrl.dismiss(); }

  displayName(s: any): string {
    if (!s) return 'Learner';
    const parts: string[] = [];
    if (s.firstName) parts.push(s.firstName);
    if (s.lastName) parts.push(s.lastName);
    if (parts.length) return parts.join(' ');
    return s.username ?? 'Learner';
  }

  openPerformance(s: any) {
    if (!s || !s.userId) return;
    this.modalCtrl.dismiss().then(() => {
      // navigate to learner performance page
      this.router.navigateByUrl(`/learner/${s.userId}`);
    });
  }

  // no search behaviour: always show a stable preview and first N entries
  private computeDisplay(): void {
    // Show up to defaultDisplayLimit learners at the front of the list
    this.display = (this.filtered || []).slice(0, this.defaultDisplayLimit);
  }

  toggleSearch(): void {
    this.showSearch = !this.showSearch;
    if (!this.showSearch) {
      // clear search
      this.query = '';
      this.filtered = (this.students || []).slice();
      this.preview = (this.filtered || []).slice(0, this.defaultDisplayLimit);
      this.computeDisplay();
    }
  }

  onSearch(ev: any): void {
    const q = (this.query || '').toLowerCase().trim();
    if (!q) {
      this.filtered = (this.students || []).slice();
    } else {
      this.filtered = (this.students || []).filter(s => {
        const parts = [s?.username, s?.firstName, s?.lastName].filter(Boolean).join(' ').toLowerCase();
        return parts.indexOf(q) !== -1;
      });
    }
    this.preview = (this.filtered || []).slice(0, this.defaultDisplayLimit);
    this.computeDisplay();
  }
}
