import { of } from 'rxjs';
import { TeacherPage } from './teacher.page';
import { ApiService } from '../../services/api.service';

describe('TeacherPage', () => {
  let mockApi: Partial<ApiService> & { createTopic: jasmine.Spy };
  let toastStub: any;
  let page: TeacherPage;

  beforeEach(() => {
    mockApi = {
      createTopic: jasmine.createSpy('createTopic').and.returnValue(of({ topicId: 't-123', storyId: 's-abc' })),
      loadAuth: () => ({ token: 't', userId: 'u1', schoolId: 'school-1', username: 'u', roles: [] })
    } as any;

    toastStub = {
      create: jasmine.createSpy('create').and.returnValue(Promise.resolve({ present: () => Promise.resolve() }))
    } as any;

    // minimal stubs for other constructor args
    const pop = {} as any;
    const modal = {} as any;
    const alert = {} as any;
    const router = {} as any;

    page = new TeacherPage(mockApi as any, toastStub, pop, modal, alert, router);
    page.schoolId = 'school-1';
    page.currentSubjectId = 'sub-1';
  });

  it('sends clientCorrelationId when saving a topic from a local story', (done) => {
    const story: any = { id: 'local-1', user: 'Topic name', extraSubtopics: [], badge: 'New' };

    // call saveTopic
    page.saveTopic(story);

    // createTopic should have been called
    expect(mockApi.createTopic).toHaveBeenCalled();
    const args = mockApi.createTopic.calls.mostRecent().args;
    const payload = args[2] as any;
    expect(payload.clientCorrelationId).toBe('local-1');

    // verify that after response the local story id was replaced with server storyId
    // createTopic returns storyId 's-abc' as set above
    // Since the subscribe is synchronous for of(), the replacement should have happened
    setTimeout(() => {
      expect(story.id).toBe('s-abc');
      expect((story as any)._localCreatedAt).toBeUndefined();
      done();
    }, 0);
  });
});
