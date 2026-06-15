import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { addIcons } from 'ionicons';
import { flameOutline, timeOutline, playOutline, heartOutline, repeatOutline, addCircleOutline } from 'ionicons/icons';
import { ModalController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../services/api.service';

interface PromptRoom {
  id: string;
  title: string;
  category: string;
  timeLeft: string;
  streakDays: number;
  participants: number;
  mood: 'violet' | 'teal' | 'amber' | 'pink';
}

interface StoryCard {
  id: string;
  promptId: string;
  user: string;
  timeAgo: string;
  reactions: number;
  remixes: number;
  badge?: string;
}

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [
    CommonModule,
    IonicModule,
  ],
  templateUrl: './feed.page.html',
  styleUrls: ['./feed.page.scss'],
})
export class FeedPage implements OnInit {
  prompts: PromptRoom[] = [
    { id: 'p1', title: 'Show your 10-sec win', category: 'Momentum', timeLeft: '12h left', streakDays: 4, participants: 182, mood: 'violet' },
    { id: 'p2', title: 'Weekend plans in 15s', category: 'Social', timeLeft: '8h left', streakDays: 2, participants: 96, mood: 'teal' },
    { id: 'p3', title: 'Desk setup snapshot', category: 'Work', timeLeft: '18h left', streakDays: 1, participants: 143, mood: 'amber' },
  ];

  stories: StoryCard[] = [
    { id: 's1', promptId: 'p1', user: 'Amina', timeAgo: '2h ago', reactions: 64, remixes: 7, badge: 'Remix-ready' },
    { id: 's2', promptId: 'p1', user: 'Luis', timeAgo: '3h ago', reactions: 48, remixes: 3 },
    { id: 's3', promptId: 'p2', user: 'Priya', timeAgo: '1h ago', reactions: 92, remixes: 11, badge: 'Trending' },
    { id: 's4', promptId: 'p3', user: 'Noah', timeAgo: '30m ago', reactions: 21, remixes: 2 },
    { id: 's5', promptId: 'p2', user: 'Maya', timeAgo: '4h ago', reactions: 35, remixes: 5 },
    { id: 's6', promptId: 'p1', user: 'Zoe', timeAgo: '10m ago', reactions: 18, remixes: 1 },
  ];

  selectedPromptId = this.prompts[0].id;
  subjects: Array<{ subjectId: string; name: string; stage?: number }> = [];
  filteredSubjects: Array<{ subjectId: string; name: string; stage?: number; subLevelId?: string | null; stageId?: string | null }> = [];
  schoolId?: string | null;
  topicCounts: Record<string, number> = {};

  constructor(private api: ApiService, private modalCtrl: ModalController) {
    addIcons({ flameOutline, timeOutline, playOutline, heartOutline, repeatOutline, addCircleOutline });
  }

  ngOnInit(): void {}

  async ngAfterViewInit(): Promise<void> {
    try {
      const auth = this.api.loadAuth?.() ?? null;
      this.schoolId = auth?.schoolId ?? null;
      if (this.schoolId) {
        try {
          const curAuth = this.api.loadAuth?.();
          console.debug('Feed.ngAfterViewInit: calling getSubjects with schoolId=', this.schoolId, 'currentAuth present=', !!curAuth, 'tokenPresent=', !!(curAuth && (curAuth as any).token));
        } catch (e) {}
          this.api.getSubjects(this.schoolId).subscribe({ next: subs => { this.subjects = subs ?? []; this.filterSubjectsForStudent().catch(() => { this.filteredSubjects = this.subjects; }).finally(() => { void this.loadTopicCounts(); }); }, error: () => { this.subjects = []; this.filteredSubjects = []; } });
      }
    } catch (err) {
      console.warn('Failed to load subjects for feed', err);
      this.subjects = [];
    }
  }

  async openStudy(subjectId?: string, subjectName?: string) {
    if (!subjectId || !this.schoolId) return;
    try {
      const m = await this.modalCtrl.create({
        component: (await import('../../components/student-learning/student-learning.component')).StudentLearningComponent,
        componentProps: { schoolId: this.schoolId, subjectId, subjectName },
        cssClass: 'student-learning-modal'
      });
      await m.present();
    } catch (e) {
      console.warn('Failed to open study modal', e);
    }
  }

  private async filterSubjectsForStudent(): Promise<void> {
    try {
      const auth = this.api.loadAuth?.() ?? null;
      const roles = (auth?.roles ?? []).map((r: any) => String(r).toLowerCase());
      // If not a student, show all subjects
      if (!roles.includes('student')) {
        this.filteredSubjects = this.subjects;
        return;
      }
      const userId = auth?.userId ?? null;
      if (!userId || !this.schoolId) {
        this.filteredSubjects = this.subjects;
        return;
      }
      const membership = await firstValueFrom(this.api.getMembership(this.schoolId, userId));
      const subLevelId = (membership as any)?.subLevelId ?? null;
      const stageId = (membership as any)?.stageId ?? null;
      if (subLevelId) {
        this.filteredSubjects = (this.subjects || []).filter(s => (s as any).subLevelId === subLevelId);
        if ((this.filteredSubjects || []).length) return;
      }
      if (stageId) {
        this.filteredSubjects = (this.subjects || []).filter(s => String(s.stage) === String(stageId) || (s as any).stageId === stageId);
        if ((this.filteredSubjects || []).length) return;
      }
      // fallback to showing all
      this.filteredSubjects = this.subjects;
    } catch (err) {
      console.warn('Failed to filter subjects for student', err);
      this.filteredSubjects = this.subjects;
    }
  }

  private async loadTopicCounts(): Promise<void> {
    try {
      const list = (this.filteredSubjects && this.filteredSubjects.length) ? this.filteredSubjects : this.subjects;
      const slice = (list || []).slice(0, 6);
      for (const s of slice) {
        try {
          const topics = await firstValueFrom(this.api.getTopics(this.schoolId ?? '', s.subjectId));
          this.topicCounts[s.subjectId] = Array.isArray(topics) ? topics.length : 0;
        } catch (e) {
          this.topicCounts[s.subjectId] = 0;
        }
      }
    } catch (e) {
      console.warn('Failed to load topic counts', e);
    }
  }

  selectPrompt(id: string): void {
    this.selectedPromptId = id;
  }

  storiesForPrompt(): StoryCard[] {
    return this.stories.filter(s => s.promptId === this.selectedPromptId);
  }

  addStory(): void {
    const mock: StoryCard = {
      id: 's' + (this.stories.length + 1),
      promptId: this.selectedPromptId,
      user: 'You',
      timeAgo: 'just now',
      reactions: 0,
      remixes: 0,
      badge: 'New',
    };
    this.stories = [mock, ...this.stories];
  }

  // Helper to safely get stage value from a subject object (avoids template type-check issues)
  getStage(s: any): string | number {
    const v = s?.stage ?? (s && s['stageId']);
    return v ?? '—';
  }
}
