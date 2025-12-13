import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  IonAvatar,
  IonButton,
  IonCard,
  IonCardContent,
  IonChip,
  IonContent,
  IonGrid,
  IonHeader,
  IonIcon,
  IonRow,
  IonCol,
  IonLabel,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { flameOutline, timeOutline, playOutline, heartOutline, repeatOutline, addCircleOutline } from 'ionicons/icons';

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
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonGrid,
    IonRow,
    IonCol,
    IonAvatar,
    IonIcon,
    IonLabel,
    IonChip,
    IonCard,
    IonCardContent,
    IonButton,
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

  constructor() {
    addIcons({ flameOutline, timeOutline, playOutline, heartOutline, repeatOutline, addCircleOutline });
  }

  ngOnInit(): void {}

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
}
