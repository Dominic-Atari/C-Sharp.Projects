import { Routes } from '@angular/router';

export const APP_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'auth',
  },
  {
    path: 'auth',
    loadComponent: () => import('./pages/auth/auth.page').then(m => m.AuthPage),
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./pages/dashboard/dashboard.page').then(m => m.DashboardPage),
  },
  {
    path: 'school/edit',
    loadComponent: () => import('./pages/school-edit/school-edit.page').then(m => m.SchoolEditPage),
  },
  {
    path: 'create/subject',
    loadComponent: () => import('./pages/create-subject/create-subject.page').then(m => m.CreateSubjectPage),
  },
  {
    path: 'create/teacher',
    loadComponent: () => import('./pages/create-teacher/create-teacher.page').then(m => m.CreateTeacherPage),
  },
  {
    path: 'create/student',
    loadComponent: () => import('./pages/create-student/create-student.page').then(m => m.CreateStudentPage),
  },
  {
    path: 'assign/teacher-subject',
    loadComponent: () => import('./pages/assign-teacher/assign-teacher.page').then(m => m.AssignTeacherPage),
  },
  {
    path: 'create/course',
    loadComponent: () => import('./pages/create-course/create-course.page').then(m => m.CreateCoursePage),
  },
  {
    path: 'create/lesson',
    loadComponent: () => import('./pages/create-lesson/create-lesson.page').then(m => m.CreateLessonPage),
  },
  {
    path: 'teacher',
    loadComponent: () => import('./pages/teacher/teacher.page').then(m => m.TeacherPage),
  },
  {
    path: 'manage/students',
    loadComponent: () => import('./pages/manage-students/manage-students.page').then(m => m.ManageStudentsPage),
  },
  {
    path: 'manage/teachers',
    loadComponent: () => import('./pages/manage-teachers/manage-teachers.page').then(m => m.ManageTeachersPage),
  },
  {
    path: 'manage/levels',
    loadComponent: () => import('./pages/manage-levels/manage-levels.page').then(m => m.ManageLevelsPage),
  },
  {
    path: 'subjects/:subjectId/topics/:topicId/performance',
    loadComponent: () => import('./pages/subject-topic-performance/subject-topic-performance.page').then(m => m.SubjectTopicPerformancePage),
  },
  {
    path: 'stages/:stageId/performance',
    loadComponent: () => import('./pages/stage-performance/stage-performance.page').then(m => m.StagePerformancePage),
  },
  {
    path: 'stages/:stageId/sublevels',
    loadComponent: () => import('./pages/stage-sublevels/stage-sublevels.page').then(m => m.StageSublevelsPage),
  },
  {
    path: 'dev/learners',
    loadComponent: () => import('./pages/dev-learners/dev-learners.page').then(m => m.DevLearnersPage),
  },
  {
    path: 'learner/:userId',
    loadComponent: () => import('./pages/learner-performance/learner-performance.page').then(m => m.LearnerPerformancePage),
  },
  {
    path: 'teacher/:subjectId',
    loadComponent: () => import('./pages/teacher/teacher.page').then(m => m.TeacherPage),
  },
  {
    path: 'admin',
    loadComponent: () => import('./pages/admin/admin.page').then(m => m.AdminPage),
  },
  {
    path: 'feed',
    loadComponent: () => import('./pages/feed/feed.page').then(m => m.FeedPage),
  },
  {
    path: '**',
    redirectTo: 'auth',
  },
];
