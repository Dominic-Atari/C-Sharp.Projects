import { Component, Input, OnInit } from '@angular/core';
import { ModalController, IonicModule } from '@ionic/angular';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface PerfGroup {
  level: string;
  average: number; // 0-100
  // each student keeps a reference to the original user (if provided)
  students: Array<{ name: string; score: number; user?: any }>;
}

@Component({
    selector: 'app-chat-performance',
    imports: [IonicModule, CommonModule, FormsModule],
    templateUrl: './chat-performance.component.html',
    styleUrls: ['./chat-performance.component.scss']
})
export class ChatPerformanceComponent implements OnInit {
  @Input() schoolId?: string | null;
  @Input() students?: Array<any> | null;

  groups: PerfGroup[] = [];
  messages: Array<{ from: 'system' | 'teacher' | 'student'; text: string; time?: string }> = [];
  draft = '';

  constructor(private modalCtrl: ModalController, private router: Router) {}

  ngOnInit(): void {
    // For now, create demo groups from provided students or synthetic data
    const seeded = (this.students && this.students.length) ? this.students.slice(0, 12).map((s: any, i: number) => ({ name: s.username || s.firstName || (`Learner ${i+1}`), score: Math.round(40 + Math.random() * 60), user: s })) :
      Array.from({ length: 12 }).map((_, i) => ({ name: `Learner ${i+1}`, score: Math.round(35 + Math.random() * 60) }));

    // bucket by score
    const beginner = seeded.filter((s: any) => s.score < 50);
    const intermediate = seeded.filter((s: any) => s.score >= 50 && s.score < 75);
    const advanced = seeded.filter((s: any) => s.score >= 75);

    this.groups = [
      { level: 'Beginner', average: beginner.length ? Math.round(beginner.reduce((a: any,b: any)=>a+b.score,0)/beginner.length) : 0, students: beginner },
      { level: 'Intermediate', average: intermediate.length ? Math.round(intermediate.reduce((a: any,b: any)=>a+b.score,0)/intermediate.length) : 0, students: intermediate },
      { level: 'Advanced', average: advanced.length ? Math.round(advanced.reduce((a: any,b: any)=>a+b.score,0)/advanced.length) : 0, students: advanced },
    ];

    // system intro messages
    this.messages.push({ from: 'system', text: `Performance snapshot for ${this.schoolId ? 'your school' : 'this class'}.`, time: new Date().toLocaleTimeString() });
    this.messages.push({ from: 'system', text: `Beginner: ${this.groups[0].students.length} • Intermediate: ${this.groups[1].students.length} • Advanced: ${this.groups[2].students.length}`, time: new Date().toLocaleTimeString() });
  }

  close() { this.modalCtrl.dismiss(); }

  send() {
    if (!this.draft.trim()) return;

    this.draft = '';
    // fake a helpful system reply
    setTimeout(() => {
      this.messages.push({ from: 'system', text: 'Tip: consider grouping learners for targeted practice based on these levels.' });
    }, 700);
  }

  openLearner(s: { user?: any; name?: string }) {
    const user = s?.user;
    if (!user?.userId) {
      console.warn('Student has no userId to navigate to', s);
      return;
    }
    // close modal then navigate to learner page
    this.modalCtrl.dismiss().then(() => void this.router.navigateByUrl(`/learner/${user.userId}`));
  }

  async openLearnerTopics(s: { user?: any; name?: string }) {
    const user = s?.user;
    if (!user?.userId) {
      console.warn('Student has no userId to open topics', s);
      return;
    }
    try {
      const m = await this.modalCtrl.create({
        component: (await import('../topic-performance/topic-performance.component')).TopicPerformanceComponent,
        componentProps: { schoolId: this.schoolId ?? null, userId: user.userId },
        cssClass: 'topic-performance-modal'
      });
      await m.present();
    } catch (e) {
      console.warn('Failed to open topic performance modal', e);
    }
  }
}
